using API.Exceptions.Handlers;

namespace API.Exceptions;

/// <summary>
///     Extension methods for registering global exception handling and
///     RFC 9457 ProblemDetails support on the <see cref="IServiceCollection" />.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    ///     Registers the exception handler chain and configures ProblemDetails
    ///     with additional diagnostic extensions on every error response.
    /// </summary>
    /// <remarks>
    ///     Exception handlers are evaluated in registration order; the first handler
    ///     that can process the exception short-circuits the chain:
    ///     <list type="number">
    ///         <item>
    ///             <description>
    ///                 <b>ValidationExceptionHandler</b> — handles FluentValidation failures
    ///                 and returns a <c>400 Bad Request</c> with field-level error details.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <b>AppExceptionHandler</b> — handles known domain/application exceptions
    ///                 and maps them to the appropriate HTTP status codes.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <b>GlobalExceptionHandler</b> — catch-all fallback that returns a generic
    ///                 <c>500 Internal Server Error</c> for any unhandled exception.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     Every ProblemDetails response is enriched with a <c>traceId</c> and a
    ///     <c>timestamp</c> extension so clients and support teams can correlate
    ///     error reports with server-side traces and logs.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" /> to configure.</param>
    /// <returns>The same <paramref name="services" /> instance to allow method chaining.</returns>
    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        // ── Exception handlers ────────────────────────────────
        // Registered most-specific first so narrower handlers get the first
        // opportunity to handle an exception before the generic fallback runs.
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<AppExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // ── ProblemDetails ────────────────────────────────────
        // Customise every RFC 9457 error response with two additional extensions:
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                // traceId lets clients include a reference in bug reports that can
                // be matched against distributed traces in the observability backend.
                ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;

                // timestamp records when the error occurred in ISO 8601 round-trip
                // format ("O"), making log correlation across time zones unambiguous.
                ctx.ProblemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("O");
            };
        });

        return services;
    }
}