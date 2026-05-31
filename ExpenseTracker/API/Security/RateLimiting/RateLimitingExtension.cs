using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Security.RateLimiting;

/// <summary>
///     Extension methods for registering and configuring rate limiting
///     on the <see cref="IServiceCollection" />.
/// </summary>
public static class RateLimitingExtension
{
    /// <summary>
    ///     Adds a global Fixed Window rate limiter built from the <c>RateLimiter</c> configuration section.
    /// </summary>
    /// <remarks>
    ///     The method is a no-op when <c>RateLimiterSettings.Enabled</c> is <c>false</c>, allowing
    ///     rate limiting to be toggled per environment without code changes.
    ///     <para>
    ///         The global limiter partitions traffic in two ways:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <b>Authenticated requests</b> — partitioned by stable user ID (<c>NameIdentifier</c> claim)
    ///                     so limits are per-user regardless of the originating IP.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <b>Anonymous requests</b> — partitioned by remote IP address, which is read directly
    ///                     from the connection rather than from <c>X-Forwarded-For</c> to prevent spoofing.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <b>Localhost (Development only)</b> — bypassed entirely to avoid hindering local testing.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///         When a request is rejected a <c>429 Too Many Requests</c> ProblemDetails response is returned
    ///         with <c>Retry-After</c> and <c>X-Rate-Limit-Reset</c> headers.
    ///     </para>
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" /> to configure.</param>
    /// <param name="env">The host environment used to bypass rate limiting for localhost in Development.</param>
    /// <param name="config">The application configuration used to bind <c>RateLimiterSettings</c>.</param>
    /// <returns>The same <paramref name="services" /> instance to allow method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the <c>RateLimiter</c> configuration section is absent.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when any numeric setting (<c>WindowMinutes</c>, <c>PermitLimit</c>,
    ///     <c>RetryAfterSeconds</c>) is not a positive integer, or <c>QueueLimit</c> is negative.
    /// </exception>
    public static IServiceCollection AddRateLimitingConfiguration(
        this IServiceCollection services,
        IHostEnvironment env,
        IConfiguration config
    )
    {
        // ── Configuration validation ───────────────────────────
        // Bind and validate eagerly so the application fails fast at startup
        // rather than silently applying incorrect rate-limit values.
        var settings = config.GetSection("RateLimiter").Get<RateLimiterSettings>();

        if (settings is null)
            throw new InvalidOperationException("RateLimiter settings are not configured properly.");

        // Allow rate limiting to be disabled entirely per environment (e.g. during integration testing)
        // without requiring separate configuration files or code changes.
        if (!settings.Enabled) return services;

        if (settings.WindowMinutes <= 0) throw new ArgumentException("WindowMinutes must be greater than 0.");
        if (settings.PermitLimit <= 0) throw new ArgumentException("PermitLimit must be greater than 0.");
        if (settings.QueueLimit < 0) throw new ArgumentException("QueueLimit cannot be negative.");
        if (settings.RetryAfterSeconds <= 0) throw new ArgumentException("RetryAfterSeconds must be greater than 0.");

        services.AddRateLimiter(options =>
        {
            // ── Rejection status code ─────────────────────────
            // Return 429 Too Many Requests (RFC 6585) instead of the ASP.NET Core
            // default of 503 Service Unavailable, which has a different semantic meaning.
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            /*
                ── Named limiter (Fixed Window) — example, not active ────────────
                Define a rate limiter named "fixed" using the Fixed Window algorithm.
                Apply it to specific endpoints with [EnableRateLimiting("fixed")]
                rather than relying on the global limiter below.

                options.AddFixedWindowLimiter("fixed", cfg =>
                {
                    cfg.PermitLimit = settings.PermitLimit;
                    cfg.Window = TimeSpan.FromMinutes(settings.WindowMinutes);
                    cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    cfg.QueueLimit = settings.QueueLimit;
                });
            */

            /*
                ── Per-user policy — example, not active ─────────────────────────
                Add a policy named "per-user" for fine-grained, identity-aware limits.
                Apply it with [EnableRateLimiting("per-user")] on controllers or actions.

                options.AddPolicy("per-user", context => { });
            */

            // ── Global limiter ────────────────────────────────
            // Applied to every request before named limiters or endpoint-level policies.
            // Uses a partitioned limiter so each client gets an independent quota.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Read the IP directly from the TCP connection to prevent clients from
                // spoofing X-Forwarded-For headers and bypassing per-IP limits.
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Bypass rate limiting for localhost in Development so local testing
                // and tooling (e.g. Swagger UI, health checks) are never throttled.
                if (env.IsDevelopment() && IsLocalhost(ip))
                    return RateLimitPartition.GetNoLimiter<string>("localhost");

                string partitionKey;

                if (context.User.Identity?.IsAuthenticated == true)
                {
                    // Partition by the stable, immutable NameIdentifier claim rather than
                    // the display name, which can change and would reset the quota unexpectedly.
                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
                    partitionKey = $"user:{userId}";
                }
                else
                {
                    // For anonymous traffic, fall back to the remote IP address as the
                    // partition key; prefix ensures no collision with user partition keys.
                    partitionKey = $"ip:{ip}";
                }

                // ── Fixed Window algorithm ────────────────────
                // Allows up to PermitLimit requests per Window duration. Excess requests
                // are queued (up to QueueLimit) and processed oldest-first before rejection.
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.PermitLimit,
                    Window = TimeSpan.FromMinutes(settings.WindowMinutes),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = settings.QueueLimit
                });
            });

            // ── Rejection handler ─────────────────────────────
            // Invoked for every request that exceeds the quota; responsible for
            // logging the event and writing the standardised 429 response.
            options.OnRejected = async (context, cancellationToken) =>
            {
                await OnRateLimitRejected(context, settings, cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    ///     Handles a rate-limited request by logging a warning and returning a
    ///     <c>429 Too Many Requests</c> ProblemDetails response with retry guidance.
    /// </summary>
    /// <param name="context">The rejection context provided by the rate-limiting middleware.</param>
    /// <param name="settings">The rate-limiter settings used to populate the retry headers.</param>
    /// <param name="ct">A cancellation token for the async response write.</param>
    private static async Task OnRateLimitRejected(
        OnRejectedContext context,
        RateLimiterSettings settings,
        CancellationToken ct
    )
    {
        // Resolve the logger via the DI container rather than injecting it into the
        // static extension class, keeping the extension stateless.
        var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("RateLimiting");

        var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var endpoint = context.HttpContext.GetEndpoint()?.DisplayName ?? "unknown endpoint";
        var path = context.HttpContext.Request.Path;
        var isAuthenticated = context.HttpContext.User.Identity?.IsAuthenticated == true;
        var userId = isAuthenticated
            ? context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown"
            : null;

        // Log with different templates so structured log sinks can distinguish
        // authenticated throttle events (user ID available) from anonymous ones.
        if (isAuthenticated)
            logger.LogWarning(
                "Rate limit exceeded for user {UserId} from IP {ClientIp} on endpoint {Endpoint} ({Path})",
                userId, clientIp, endpoint, path
            );
        else
            logger.LogWarning(
                "Rate limit exceeded for anonymous user from IP {ClientIp} on endpoint {Endpoint} ({Path})",
                clientIp, endpoint, path
            );

        // ── Reset time calculation ────────────────────────────
        // Align the reset timestamp to the actual window boundary (e.g. the next full
        // minute mark) rather than using a static offset from now, giving clients an
        // accurate time after which their quota will be restored.
        var windowSeconds = (int)TimeSpan.FromMinutes(settings.WindowMinutes).TotalSeconds;
        var nowEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var resetEpoch = nowEpoch + (windowSeconds - nowEpoch % windowSeconds);
        var resetTime = DateTimeOffset.FromUnixTimeSeconds(resetEpoch);

        // ── Response headers ──────────────────────────────────
        // Retry-After (RFC 7231) tells clients how many seconds to wait before retrying.
        // X-Rate-Limit-Reset provides the absolute UTC reset time in ISO 8601 format
        // for clients that prefer to schedule retries against a wall-clock timestamp.
        context.HttpContext.Response.Headers.RetryAfter = settings.RetryAfterSeconds.ToString();
        context.HttpContext.Response.Headers.Append("X-Rate-Limit-Reset", resetTime.ToString("o"));

        // ── ProblemDetails response body ──────────────────────
        // Only write the body if the response has not already started streaming;
        // writing to a started response would corrupt it.
        if (!context.HttpContext.Response.HasStarted)
        {
            context.HttpContext.Response.ContentType = "application/json";

            // Use ProblemDetailsFactory so the response shape is consistent with
            // all other error responses produced by the exception-handling pipeline.
            var problemDetailsFactory = context.HttpContext.RequestServices
                .GetRequiredService<ProblemDetailsFactory>();

            var response = problemDetailsFactory.CreateProblemDetails(
                context.HttpContext,
                StatusCodes.Status429TooManyRequests,
                "Too Many Requests",
                detail: $"You have exceeded the allowed number of requests. " +
                        $"Please wait {settings.RetryAfterSeconds} seconds before trying again." +
                        $"Reset time: {resetTime:o}"
            );

            await context.HttpContext.Response.WriteAsJsonAsync(response, ct);

            /*await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests. Please try again later.",
                message = $"You have exceeded the allowed number of requests. " +
                          $"Please wait {settings.RetryAfterSeconds} seconds before trying again.",
                statusCode = StatusCodes.Status429TooManyRequests,
                retryAfterSeconds = settings.RetryAfterSeconds,
                resetTime = resetTime.ToString("o")
            }, ct);*/
        }
    }

    /// <summary>
    ///     Returns <c>true</c> when the supplied IP address string represents a loopback
    ///     (localhost) address, used to bypass rate limiting in the Development environment.
    /// </summary>
    /// <param name="ip">The remote IP address string to evaluate.</param>
    private static bool IsLocalhost(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return false;

        // Cover IPv6 loopback (::1), standard IPv4 loopback (127.0.0.1),
        // the literal "localhost" hostname, and the full 127.x.x.x range.
        return ip is "::1" or "127.0.0.1" or "localhost" || ip.StartsWith("127.");
    }
}