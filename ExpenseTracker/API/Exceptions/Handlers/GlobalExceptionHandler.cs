using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.Exceptions.Handlers;

/// <summary>
/// Catch-all exception handler that processes any unhandled exception not
/// claimed by a more specific handler earlier in the chain.
/// </summary>
/// <remarks>
/// This handler is registered last in the exception handler chain (after
/// <c>ValidationExceptionHandler</c> and <c>AppExceptionHandler</c>) and always
/// returns a generic <c>500 Internal Server Error</c> ProblemDetails response.
/// The internal exception details are intentionally omitted from the response
/// body to avoid leaking sensitive implementation information to clients;
/// the full exception is captured in the structured log instead.
/// </remarks>
/// <param name="details">The <see cref="IProblemDetailsService"/> used to write the response body.</param>
/// <param name="logger">The logger used to record the full exception at error level.</param>
public sealed class GlobalExceptionHandler(IProblemDetailsService details, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    /// <summary>
    /// Handles any unhandled exception by logging it and returning a
    /// <c>500 Internal Server Error</c> RFC 9457 ProblemDetails response.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="exception">The unhandled exception to process.</param>
    /// <param name="ct">A cancellation token for the async response write.</param>
    /// <returns>
    /// Always returns <c>true</c> after writing the response, signalling that
    /// the exception has been handled and no further handlers should run.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct
    )
    {
        // Log at Error level — unlike AppException (expected failures), anything
        // reaching this handler represents a genuinely unexpected server-side fault
        // that requires investigation. The full exception is captured here since it
        // is intentionally withheld from the client response body.
        logger.LogError(
            exception,
            "Unhandled exception occurred. Path: {Path} Method: {Method}",
            httpContext.Request.Path,
            httpContext.Request.Method);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // ── ProblemDetails response ────────────────────────────
        // Return a generic, safe message to the client. The RFC 7231 §6.6.1 type
        // URI gives consumers a stable, machine-readable identifier for 500 errors
        // without revealing any internal stack trace or exception details.
        return await details.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred. Please try again later.",
                Instance = httpContext.Request.Path
            }
        });
    }
}