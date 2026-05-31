using Application.Exceptions;
using Application.Exceptions.Custom;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.Exceptions.Handlers;

/// <summary>
///     Handles known application exceptions (<see cref="AppException" /> and its subtypes)
///     by mapping them to RFC 9457 ProblemDetails HTTP responses.
/// </summary>
/// <remarks>
///     This handler sits in the middle of the exception handler chain — after
///     <c>ValidationExceptionHandler</c> (more specific) and before
///     <c>GlobalExceptionHandler</c> (catch-all fallback). It returns <c>false</c>
///     for any exception that is not an <see cref="AppException" />, allowing the
///     next handler in the chain to process it.
///     <para>
///         For <see cref="BusinessRuleException" /> specifically, a <c>rule</c> extension
///         property is added to the ProblemDetails body so clients can programmatically
///         identify which business rule was violated without parsing the detail message.
///     </para>
/// </remarks>
/// <param name="details">The <see cref="IProblemDetailsService" /> used to write the response body.</param>
/// <param name="logger">The logger used to record a warning for each handled exception.</param>
public sealed class AppExceptionHandler(IProblemDetailsService details, ILogger<AppExceptionHandler> logger)
    : IExceptionHandler
{
    /// <summary>
    ///     Attempts to handle the exception if it is an <see cref="AppException" />.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="ct">A cancellation token for the async response write.</param>
    /// <returns>
    ///     <c>true</c> if the exception was an <see cref="AppException" /> and the response
    ///     was written successfully; <c>false</c> to pass the exception to the next handler.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct
    )
    {
        // Only handle AppException and its subtypes; return false so the next
        // registered handler (GlobalExceptionHandler) processes everything else.
        if (exception is not AppException appException)
            return false;

        // Log at Warning level — application exceptions represent expected failure
        // scenarios (e.g. not found, conflict) rather than unexpected server errors.
        logger.LogWarning(
            exception,
            "Application exception occurred. StatusCode: {StatusCode} Path: {Path}",
            appException.StatusCode,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = appException.StatusCode;

        // ── ProblemDetails response ────────────────────────────
        // Build a standardised RFC 9457 response body using the status code to
        // derive the appropriate RFC hyperlink and human-readable title.
        var problemDetails = new ProblemDetails
        {
            Type = GetRfcLink(appException.StatusCode),
            Title = GetTitle(appException.StatusCode),
            Status = appException.StatusCode,
            Detail = appException.Message,
            Instance = httpContext.Request.Path
        };

        // Attach the violated rule name as an extension property for BusinessRuleException
        // so API clients can react to specific rule failures without string-matching the detail.
        if (appException is BusinessRuleException businessRule)
            problemDetails.Extensions["rule"] = businessRule.Rule;

        return await details.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    /// <summary>
    ///     Returns the RFC hyperlink that describes the semantics of the given HTTP status code,
    ///     used as the <c>type</c> field in the ProblemDetails response.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to look up.</param>
    private static string GetRfcLink(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1", // Bad Request
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1", // Unauthorized
            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3", // Forbidden
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4", // Not Found
            409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8", // Conflict
            422 => "https://tools.ietf.org/html/rfc4918#section-11.2", // Unprocessable Entity
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1" // Internal Server Error (fallback)
        };
    }

    /// <summary>
    ///     Returns a short, human-readable title for the given HTTP status code,
    ///     used as the <c>title</c> field in the ProblemDetails response.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to look up.</param>
    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            _ => "Internal Server Error"
        };
    }
}