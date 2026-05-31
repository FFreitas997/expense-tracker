using API.Observability.Settings;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace API.Observability.Tracing;

/// <summary>
///     Extension methods for registering OpenTelemetry distributed tracing
///     on the <see cref="IServiceCollection" />.
/// </summary>
public static class TracingExtensions
{
    /// <summary>
    ///     Configures OpenTelemetry tracing using settings sourced from the
    ///     <c>ObservabilitySettings</c> configuration section.
    /// </summary>
    /// <remarks>
    ///     The method performs three configuration phases:
    ///     <list type="number">
    ///         <item>
    ///             <description>
    ///                 <b>Resource</b> — attaches service identity metadata (name, version,
    ///                 deployment environment) to every exported span so backends such as
    ///                 Jaeger or Tempo can group traces by service.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <b>Instrumentation</b> — automatically creates spans for ASP.NET Core
    ///                 requests, outbound <see cref="System.Net.Http.HttpClient" /> calls, and
    ///                 Entity Framework Core database commands.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <b>Exporters</b> — always sends traces via OTLP; optionally also writes
    ///                 to the console when <c>EnableConsoleExporter</c> is <c>true</c>.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" /> to configure.</param>
    /// <param name="configuration">The application configuration used to bind <c>ObservabilitySettings</c>.</param>
    /// <returns>The same <paramref name="services" /> instance to allow method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the <c>ObservabilitySettings</c> section is absent from configuration.
    /// </exception>
    public static IServiceCollection AddAppTracing(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind the observability settings; fail fast so misconfigured deployments
        // surface the problem at startup rather than silently dropping traces.
        var settings = configuration.GetSection("ObservabilitySettings").Get<ObservabilitySettings>();

        if (settings is null)
            throw new InvalidOperationException("ObservabilitySettings section is missing in configuration.");

        // Register ObservabilitySettings in the DI container with data-annotation validation
        // so [Required], [MinLength], and [Url] constraints are enforced at startup.
        services
            .AddOptions<ObservabilitySettings>()
            .Bind(configuration.GetSection("ObservabilitySettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOpenTelemetry()
            // ── Resource ─────────────────────────────────────
            // Attach service identity attributes that will be included on every span.
            // The deployment.environment attribute follows the OpenTelemetry semantic
            // conventions and allows filtering traces by environment in the backend.
            .ConfigureResource(resource => resource
                .AddService(settings.ServiceName, serviceVersion: settings.ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = settings.ServiceEnvironment
                }))
            .WithTracing(tracing =>
            {
                tracing
                    // Register the application's own ActivitySource so that custom
                    // spans created via AppActivitySource.Instance are captured.
                    .AddSource(AppActivitySource.Name)

                    // ── Instrumentation ──────────────────────
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Attach exception details to spans so failures are visible
                        // in the trace without having to cross-reference logs.
                        options.RecordException = true;

                        // Exclude health-check endpoints to avoid polluting the trace
                        // backend with high-frequency, low-value liveness probe spans.
                        options.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                    })
                    // Capture outbound HTTP calls (e.g. third-party APIs) as child spans,
                    // including any exceptions that occur during the request.
                    .AddHttpClientInstrumentation(options => options.RecordException = true)
                    // Capture EF Core database commands as child spans, giving visibility
                    // into query performance without manual instrumentation.
                    .AddEntityFrameworkCoreInstrumentation();

                // ── Exporters ────────────────────────────────
                // Console exporter is useful during local development for inspecting
                // span output without requiring a running collector.
                if (settings.EnableConsoleExporter)
                    tracing.AddConsoleExporter();

                // OTLP exporter sends traces to a collector (e.g. OpenTelemetry Collector,
                // Jaeger, Tempo) using the endpoint defined in configuration.
                tracing.AddOtlpExporter(options => options.Endpoint = new Uri(settings.Otlp.Endpoint));
            });

        return services;
    }
}