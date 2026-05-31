using System.Diagnostics;

namespace API.Middlewares;

/// <summary>
/// Middleware that logs every incoming HTTP request along with its outcome
/// and elapsed processing time, providing basic observability for the API.
/// </summary>
/// <remarks>
/// On success, an <c>Information</c> entry is written that includes the HTTP
/// method, path, query string, response status code, and duration in milliseconds.
/// On an unhandled exception, an <c>Error</c> entry is written before the
/// exception is re-thrown so that upstream error-handling middleware can still
/// process it normally.
/// </remarks>
/// <param name="next">The next middleware delegate in the pipeline.</param>
/// <param name="logger">The logger instance used to write request log entries.</param>
public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    /// <summary>
    /// Processes the HTTP request, measures its duration, and logs the result.
    /// </summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Start timing immediately so the measured duration covers the entire
        // downstream pipeline, including any subsequent middleware and handlers.
        var sw = Stopwatch.StartNew();

        try
        {
            await next(context);
            sw.Stop();

            // Log a structured entry once the rest of the pipeline has completed
            // successfully. Using structured logging tokens (e.g. {Method}) allows
            // log sinks such as Seq or Elasticsearch to index each property individually.
            logger.LogInformation(
                "HTTP {Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
        catch
        {
            sw.Stop();

            // Log an error with the elapsed time before re-throwing so that the
            // exception continues to propagate to the global exception handler.
            // The query string is intentionally omitted here to keep the error
            // entry concise; the full details are available in the exception itself.
            logger.LogError(
                "HTTP {Method} {Path} failed after {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                sw.ElapsedMilliseconds);

            throw;
        }
    }
}