using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.Exceptions.Handlers;

public sealed class GlobalExceptionHandler(IProblemDetailsService details, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct
    )
    {
        logger.LogError(
            exception,
            "Unhandled exception occurred. Path: {Path} Method: {Method}",
            httpContext.Request.Path,
            httpContext.Request.Method);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

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