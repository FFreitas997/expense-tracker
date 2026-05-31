using System.ComponentModel.DataAnnotations;

namespace API.Observability.Settings;

/// <summary>
/// Strongly-typed settings bound from the <c>ObservabilitySettings</c> configuration section,
/// used to configure structured logging and OpenTelemetry tracing.
/// </summary>
public sealed class ObservabilitySettings
{
    /// <summary>Logical name of the service, included in every log event and trace span.</summary>
    [Required, MinLength(1)]
    public string ServiceName { get; set; } = "expense-tracker-api";

    /// <summary>Semantic version of the service, reported as a resource attribute on spans.</summary>
    [Required, MinLength(1)]
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>Deployment environment name (e.g. <c>development</c>, <c>production</c>), reported as a resource attribute.</summary>
    [Required, MinLength(1)]
    public string ServiceEnvironment { get; set; } = "development";

    /// <summary>
    /// When <c>true</c>, an additional OpenTelemetry console exporter is registered.
    /// Useful during local development; should be <c>false</c> in production.
    /// </summary>
    public bool EnableConsoleExporter { get; set; } = false;

    /// <summary>OTLP exporter settings used to send traces to a collector endpoint.</summary>
    [Required]
    public OtlpSettings Otlp { get; set; } = new();

    /// <summary>Structured logging settings for Serilog sink configuration.</summary>
    [Required]
    public LoggingSettings Logging { get; set; } = new();
}

/// <summary>
/// Settings for the OpenTelemetry Protocol (OTLP) exporter used to
/// ship traces to a collector such as the OpenTelemetry Collector, Jaeger, or Tempo.
/// </summary>
public sealed class OtlpSettings
{
    /// <summary>
    /// The gRPC or HTTP endpoint of the OTLP collector.
    /// Must be an absolute URI (e.g. <c>http://localhost:4317</c>).
    /// </summary>
    [Required, Url]
    public string Endpoint { get; set; } = "http://localhost:4317";
}

/// <summary>
/// Settings for the Serilog logging pipeline, controlling the minimum
/// log level and optional rolling-file output.
/// </summary>
public sealed class LoggingSettings
{
    /// <summary>
    /// The minimum Serilog <c>LogEventLevel</c> name (e.g. <c>Information</c>, <c>Warning</c>).
    /// Parsed case-insensitively at startup; invalid values will throw at configuration time.
    /// </summary>
    [Required, MinLength(1)]
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// The file path template for the rolling-file sink
    /// (e.g. <c>logs/expense-tracker-.log</c>). Only used when <see cref="EnableFileOutput"/> is <c>true</c>.
    /// </summary>
    [Required, MinLength(1)]
    public string FilePath { get; set; } = "logs/expense-tracker-.log";

    /// <summary>
    /// When <c>true</c>, Serilog writes log events to a daily rolling file
    /// in addition to the console sink.
    /// </summary>
    public bool EnableFileOutput { get; set; } = true;
}