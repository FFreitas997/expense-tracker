using System.Security.Claims;
using System.Threading.RateLimiting;
using API.Settings;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Extensions;

public static class RateLimitingExtension
{
    public static IServiceCollection AddRateLimitingConfiguration(
        this IServiceCollection services,
        IHostEnvironment env,
        IConfiguration config
    )
    {
        var settings = config.GetSection("RateLimiter").Get<RateLimiterSettings>();

        if (settings is null)
            throw new InvalidOperationException("RateLimiter settings are not configured properly.");

        if (!settings.Enabled) return services;

        if (settings.WindowMinutes <= 0) throw new ArgumentException("WindowMinutes must be greater than 0.");
        if (settings.PermitLimit <= 0) throw new ArgumentException("PermitLimit must be greater than 0.");
        if (settings.QueueLimit < 0) throw new ArgumentException("QueueLimit cannot be negative.");
        if (settings.RetryAfterSeconds <= 0) throw new ArgumentException("RetryAfterSeconds must be greater than 0.");

        services.AddRateLimiter(options =>
        {
            // Set the default rejection status code to 429 Too Many Requests
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            /*
                Define a rate limiter named "fixed" using the Fixed Window algorithm with settings from configuration.
                You must apply this limiter to specific endpoints or globally as needed.

                options.AddFixedWindowLimiter("fixed", cfg =>
                {
                    cfg.PermitLimit = settings.PermitLimit;
                    cfg.Window = TimeSpan.FromMinutes(settings.WindowMinutes);
                    cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    cfg.QueueLimit = settings.QueueLimit;
                });
            */


            /*
                Add a custom policy named "per-user" that applies rate limiting based on the authenticated user's ID.
                You can implement the logic inside the lambda to create a partition key based on the user's identity.
                To apply this policy, you would use the [EnableRateLimiting("per-user")] attribute on your controllers or actions.

                options.AddPolicy("per-user", context => { });
            */

            // Configure a global rate limiter that applies to all requests
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Use RemoteIpAddress directly — never trust X-Forwarded-For without validated ForwardedHeaders middleware
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Allow unlimited requests from localhost during development to avoid hindering local testing
                if (env.IsDevelopment() && IsLocalhost(ip))
                    return RateLimitPartition.GetNoLimiter<string>("localhost");

                string partitionKey;

                if (context.User.Identity?.IsAuthenticated == true)
                {
                    // Use stable, immutable user ID (NameIdentifier) rather than the mutable username
                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
                    partitionKey = $"user:{userId}";
                }
                else
                {
                    partitionKey = $"ip:{ip}";
                }

                // Rate limit using algorithm: Fixed Window
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.PermitLimit,
                    Window = TimeSpan.FromMinutes(settings.WindowMinutes),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = settings.QueueLimit
                });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                await OnRateLimitRejected(context, settings, cancellationToken);
            };
        });

        return services;
    }

    private static async Task OnRateLimitRejected(
        OnRejectedContext context,
        RateLimiterSettings settings,
        CancellationToken ct
    )
    {
        var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("RateLimiting");

        var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var endpoint = context.HttpContext.GetEndpoint()?.DisplayName ?? "unknown endpoint";
        var path = context.HttpContext.Request.Path;
        var isAuthenticated = context.HttpContext.User.Identity?.IsAuthenticated == true;
        var userId = isAuthenticated
            ? context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown"
            : null;

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

        // Align reset time to the actual window boundary rather than a static offset
        var windowSeconds = (int)TimeSpan.FromMinutes(settings.WindowMinutes).TotalSeconds;
        var nowEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var resetEpoch = nowEpoch + (windowSeconds - nowEpoch % windowSeconds);
        var resetTime = DateTimeOffset.FromUnixTimeSeconds(resetEpoch);

        context.HttpContext.Response.Headers.RetryAfter = settings.RetryAfterSeconds.ToString();
        context.HttpContext.Response.Headers.Append("X-Rate-Limit-Reset", resetTime.ToString("o"));

        if (!context.HttpContext.Response.HasStarted)
        {
            context.HttpContext.Response.ContentType = "application/json";

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

    private static bool IsLocalhost(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return false;

        return ip is "::1" or "127.0.0.1" or "localhost" || ip.StartsWith("127.");
    }
}