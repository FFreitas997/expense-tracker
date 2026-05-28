using Application.Exceptions;
using Application.Exceptions.Custom;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.Exceptions.Handlers;

public sealed class AppExceptionHandler(IProblemDetailsService details, ILogger<AppExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct
    )
    {
        if (exception is not AppException appException)
            return false;

        logger.LogWarning(
            exception,
            "Application exception occurred. StatusCode: {StatusCode} Path: {Path}",
            appException.StatusCode,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = appException.StatusCode;

        var problemDetails = new ProblemDetails
        {
            Type = GetRfcLink(appException.StatusCode),
            Title = GetTitle(appException.StatusCode),
            Status = appException.StatusCode,
            Detail = appException.Message,
            Instance = httpContext.Request.Path
        };

        // Attach extra info for BusinessRuleException
        if (appException is BusinessRuleException businessRule)
            problemDetails.Extensions["rule"] = businessRule.Rule;

        return await details.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    private static string GetRfcLink(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }

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
            _ => "An error occurred"
        };
    }
}