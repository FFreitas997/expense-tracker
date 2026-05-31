using System.Security.Claims;
using Serilog.Context;

namespace API.Middlewares;

/// <summary>
/// Middleware that ensures every HTTP request carries a correlation ID
/// for distributed tracing and structured logging purposes.
/// </summary>
/// <remarks>
/// If the incoming request already contains an <c>X-Correlation-ID</c> header,
/// that value is reused; otherwise a new <see cref="Guid"/> is generated.
/// The correlation ID is forwarded on the response and pushed into the
/// Serilog <see cref="LogContext"/> together with the authenticated user ID,
/// so every log entry emitted during the request lifecycle is automatically
/// enriched with both properties.
/// </remarks>
/// <param name="next">The next middleware delegate in the pipeline.</param>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    // The standard header name used to propagate the correlation ID across services.
    private const string Header = "X-Correlation-ID";

    /// <summary>
    /// Processes the HTTP request by resolving or generating a correlation ID,
    /// enriching the Serilog log context, and invoking the next middleware.
    /// </summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Reuse the caller-supplied correlation ID or generate a fresh one.
        var correlationId = context.Request.Headers[Header].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        // Store the correlation ID in HttpContext.Items so it is accessible
        // anywhere within the current request pipeline without re-reading the header.
        context.Items[Header] = correlationId;

        // Echo the correlation ID back to the caller so it can be matched
        // against server-side logs when diagnosing issues.
        context.Response.Headers.TryAdd(Header, correlationId);

        // Push both the correlation ID and the authenticated user ID into the
        // Serilog LogContext for the duration of the request. The using blocks
        // ensure the properties are removed from the context once the request ends.
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId",
                   context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous"))
        {
            await next(context);
        }
    }
}