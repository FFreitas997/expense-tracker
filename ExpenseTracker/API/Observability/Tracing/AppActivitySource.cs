using System.Diagnostics;

namespace API.Observability.Tracing;

/// <summary>
///     Provides a shared <see cref="ActivitySource" /> used to create custom
///     distributed tracing spans throughout the application.
/// </summary>
/// <remarks>
///     Centralising the <see cref="ActivitySource" /> here ensures that all
///     manually instrumented code references the same source name, which must
///     also be registered via <c>AddSource</c> in <see cref="TracingExtensions.AddAppTracing" />
///     for the spans to be collected and exported.
/// </remarks>
public static class AppActivitySource
{
    /// <summary>The name of the activity source, used for registration and filtering.</summary>
    public const string Name = "ExpenseTracker";

    /// <summary>
    ///     The singleton <see cref="ActivitySource" /> instance used to start new
    ///     <see cref="Activity" /> spans across the application.
    /// </summary>
    public static readonly ActivitySource Instance = new(Name, "1.0.0");
}