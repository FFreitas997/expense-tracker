using Application.Exceptions.Custom;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.Exceptions.Handlers;

/// <summary>
/// Handles <see cref="ValidationException"/> instances by returning a
/// <c>400 Bad Request</c> RFC 9457 <see cref="ValidationProblemDetails"/> response
/// that includes field-level validation error details.
/// </summary>
/// <remarks>
/// This handler is registered first in the exception handler chain so that
/// validation failures are intercepted before the more generic
/// <c>AppExceptionHandler</c> and <c>GlobalExceptionHandler</c> run.
/// It returns <c>false</c> for any exception that is not a
/// <see cref="ValidationException"/>, passing control to the next handler.
/// <para>
/// <see cref="ValidationProblemDetails"/> is used instead of plain
/// <see cref="ProblemDetails"/> so the <c>errors</c> extension property is
/// populated with a dictionary of field names to error messages, giving API
/// clients the granular feedback they need to surface form-level errors.
/// </para>
/// </remarks>
/// <param name="details">The <see cref="IProblemDetailsService"/> used to write the response body.</param>
public sealed class ValidationExceptionHandler(IProblemDetailsService details) : IExceptionHandler
{
    /// <summary>
    /// Attempts to handle the exception if it is a <see cref="ValidationException"/>.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="cancellationToken">A cancellation token for the async response write.</param>
    /// <returns>
    /// <c>true</c> if the exception was a <see cref="ValidationException"/> and the response
    /// was written successfully; <c>false</c> to pass the exception to the next handler.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        // Only handle ValidationException; return false so the next registered
        // handler (AppExceptionHandler) processes all other exception types.
        if (exception is not ValidationException validationException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        // ── ValidationProblemDetails response ─────────────────
        // Pass the Errors dictionary from the ValidationException directly into
        // ValidationProblemDetails so the response body contains a structured
        // per-field breakdown (e.g. { "Email": ["must not be empty"] }) that
        // clients can map directly to form validation messages.
        return await details.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ValidationProblemDetails(validationException.Errors)
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", // Bad Request
                Title = "Validation Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = validationException.Message
            }
        });
    }
}