using API.Observability.Settings;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace API.Observability.Logging;

/// <summary>
///     Extension methods for registering structured logging with Serilog
///     on the <see cref="WebApplicationBuilder" />.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    ///     Configures Serilog as the application's logging provider using settings
    ///     sourced from the <c>ObservabilitySettings</c> configuration section.
    /// </summary>
    /// <remarks>
    ///     The method performs three configuration phases:
    ///     <list type="number">
    ///         <item>
    ///             <description>
    ///                 <b>Minimum levels</b> — sets the global floor from configuration and
    ///                 overrides noisy framework namespaces to <c>Warning</c> to reduce log volume.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <b>Enrichers</b> — attaches contextual properties (correlation ID, machine
    ///                 name, thread ID, service metadata) to every log event automatically.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <b>Sinks</b> — always writes to the console in compact JSON format; optionally
    ///                 writes to a rolling file when <c>EnableFileOutput</c> is <c>true</c>.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <param name="builder">The <see cref="WebApplicationBuilder" /> to configure.</param>
    /// <returns>The same <paramref name="builder" /> instance to allow method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the <c>ObservabilitySettings</c> section is absent from configuration.
    /// </exception>
    public static WebApplicationBuilder AddStructuredLogging(this WebApplicationBuilder builder)
    {
        // Bind the observability settings from configuration; fail fast if the section
        // is missing so misconfigured deployments surface the problem immediately.
        var settings = builder.Configuration
            .GetSection("ObservabilitySettings")
            .Get<ObservabilitySettings>();

        if (settings is null)
            throw new InvalidOperationException("ObservabilitySettings section is missing in configuration.");

        // Parse the minimum level string (e.g. "Information") into the Serilog enum,
        // using case-insensitive parsing to be tolerant of configuration casing differences.
        var minimumLevel = Enum.Parse<LogEventLevel>(settings.Logging.MinimumLevel, true);

        Log.Logger = new LoggerConfiguration()

            // ── Minimum levels ───────────────────────────────
            // Apply the configured global minimum level, then suppress chattier
            // framework namespaces to Warning to avoid flooding the log output.
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Quartz", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)

            // ── Enrichers ────────────────────────────────────
            // FromLogContext picks up properties pushed via LogContext.PushProperty
            // (e.g. CorrelationId and UserId added by CorrelationIdMiddleware).
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName() // e.g. "Production", "Development"
            .Enrich.WithMachineName() // useful for identifying the host in multi-instance deployments
            .Enrich.WithThreadId() // helps correlate concurrent request logs
            .Enrich.WithCorrelationId() // propagates the X-Correlation-ID header value
            .Enrich.WithProperty("ServiceName", settings.ServiceName)
            .Enrich.WithProperty("ServiceVersion", settings.ServiceVersion)
            .Enrich.WithProperty("Environment", settings.ServiceEnvironment)

            // ── Sinks ────────────────────────────────────────
            // Console sink uses RenderedCompactJsonFormatter so container log
            // aggregators (e.g. Fluentd, Logstash) can parse events as JSON.
            .WriteTo.Console(new RenderedCompactJsonFormatter())
            // File sink is conditional: only activated when EnableFileOutput is true,
            // keeping the configuration self-contained without requiring separate
            // environment-specific Serilog JSON blocks.
            .WriteTo.Conditional(
                _ => settings.Logging.EnableFileOutput,
                wt => wt.File(
                    new RenderedCompactJsonFormatter(),
                    settings.Logging.FilePath,
                    rollingInterval: RollingInterval.Day, // new file created each day
                    retainedFileCountLimit: 30, // keep the last 30 daily files
                    fileSizeLimitBytes: 100 * 1024 * 1024, // 100 MB per file
                    rollOnFileSizeLimit: true)) // roll to a new file when the size limit is hit
            .CreateLogger();

        // Replace the default .NET logging pipeline with Serilog so all ILogger<T>
        // instances in the application funnel through the configuration above.
        builder.Host.UseSerilog();

        return builder;
    }
}