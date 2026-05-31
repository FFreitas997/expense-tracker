using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace API.Security.Authentication;

/// <summary>
///     Extension methods for registering JWT Bearer authentication
///     on the <see cref="IServiceCollection" />.
/// </summary>
public static class AuthenticationExtension
{
    /// <summary>
    ///     Adds JWT Bearer authentication using settings sourced from the <c>Jwt</c> configuration section.
    /// </summary>
    /// <remarks>
    ///     The configuration is split into three phases:
    ///     <list type="number">
    ///         <item>
    ///             <description>
    ///                 <b>Scheme defaults</b> — overrides ASP.NET Core Identity's default cookie scheme
    ///                 so that every authenticate, challenge, and sign-in operation uses JWT Bearer.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <b>Token validation</b> — validates issuer, audience, lifetime, and signing key;
    ///                 sets <c>ClockSkew</c> to zero so expired tokens are rejected immediately.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <b>Events</b> — hooks into the JWT pipeline to log failures and successes, and
    ///                 to return RFC 9457 ProblemDetails bodies for <c>401</c> and <c>403</c> responses
    ///                 instead of the default redirect behaviour.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" /> to configure.</param>
    /// <param name="config">The application configuration used to bind <c>JwtSettings</c>.</param>
    /// <returns>The same <paramref name="services" /> instance to allow method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the <c>Jwt</c> configuration section is absent.
    /// </exception>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        // ── Configuration validation ───────────────────────────
        // Bind and validate eagerly so a missing JWT section surfaces at startup
        // rather than causing cryptic NullReferenceExceptions at runtime.
        var settings = config.GetSection("Jwt").Get<JwtSettings>();

        if (settings is null)
            throw new InvalidOperationException("JWT settings are not configured properly.");

        // Register JwtSettings in the DI container so application services
        // (e.g. token generation in the Infrastructure layer) can inject IOptions<JwtSettings>.
        // ValidateDataAnnotations enforces [Required], [MinLength], and [Range] attributes at
        // startup via ValidateOnStart, preventing the application from running with invalid config.
        services
            .AddOptions<JwtSettings>()
            .Bind(config.GetSection("Jwt"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddAuthentication(options =>
            {
                // ── Scheme defaults ───────────────────────────
                // Override ASP.NET Core Identity's default cookie-based schemes so
                // all authentication, challenge, and sign-in operations use JWT Bearer.
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Reject tokens delivered over plain HTTP to prevent token interception.
                options.RequireHttpsMetadata = true;

                // Do not store the raw token in AuthenticationProperties; the application
                // reads claims directly from the ClaimsPrincipal instead.
                options.SaveToken = false;

                // ── Token validation parameters ───────────────
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // ── What to validate ──────────────────────
                    ValidateIssuer = true, // reject tokens from unknown issuers
                    ValidateAudience = true, // reject tokens not intended for this API
                    ValidateLifetime = true, // reject expired tokens
                    ValidateIssuerSigningKey = true, // reject tokens with an invalid signature

                    // ── Expected values ───────────────────────
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),

                    // No tolerance on expiry — tokens are invalid the instant they expire,
                    // preventing a window of abuse that the default 5-minute skew allows.
                    ClockSkew = TimeSpan.Zero,

                    // ── Claim type mapping ────────────────────
                    // Map to System.Security.Claims constants so HttpContext.User.Identity.Name
                    // and role-based [Authorize(Roles = "...")] work without custom mapping.
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };

                // ── JWT pipeline events ───────────────────────
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = OnAuthenticationFailed,
                    OnTokenValidated = OnTokenValidated,
                    OnChallenge = OnChallenge,
                    OnForbidden = OnForbidden
                };
            });

        return services;
    }

    /// <summary>
    ///     Logs a warning whenever JWT authentication fails (e.g. invalid signature,
    ///     expired token, malformed header) to aid in diagnosing authentication issues.
    /// </summary>
    /// <param name="ctx">The context supplied by the JWT Bearer middleware on failure.</param>
    private static Task OnAuthenticationFailed(AuthenticationFailedContext ctx)
    {
        // Resolve the logger via DI to keep the static extension class stateless.
        var logger = ctx.HttpContext.RequestServices
            .GetRequiredService<ILogger<JwtBearerEvents>>();

        // Log the full exception so the root cause (e.g. key mismatch, clock drift)
        // is visible in structured log sinks without needing to reproduce the request.
        logger.LogWarning(
            ctx.Exception,
            "JWT authentication failed. Path: {Path}",
            ctx.HttpContext.Request.Path);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Logs a debug entry after a JWT token has been successfully validated,
    ///     recording the authenticated user ID and the requested path.
    /// </summary>
    /// <param name="ctx">The context supplied by the JWT Bearer middleware on successful validation.</param>
    private static Task OnTokenValidated(TokenValidatedContext ctx)
    {
        var logger = ctx.HttpContext.RequestServices
            .GetRequiredService<ILogger<JwtBearerEvents>>();

        // Use NameIdentifier (stable user ID) rather than the display name claim,
        // which may be absent or ambiguous for service accounts.
        var userId = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        logger.LogDebug(
            "JWT validated for user {UserId}. Path: {Path}",
            userId,
            ctx.HttpContext.Request.Path);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handles the JWT Bearer challenge event by suppressing the default redirect
    ///     and returning a <c>401 Unauthorized</c> RFC 9457 ProblemDetails response instead.
    /// </summary>
    /// <param name="ctx">The context supplied by the JWT Bearer middleware when a challenge is issued.</param>
    private static Task OnChallenge(JwtBearerChallengeContext ctx)
    {
        // Prevent the middleware from writing its own WWW-Authenticate redirect response
        // so the global exception handler and this handler remain the sole response writers.
        ctx.HandleResponse();

        // Only write the body if the response has not already started streaming;
        // writing to a started response would corrupt it.
        if (!ctx.Response.HasStarted)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.ContentType = "application/problem+json";

            // Build a ProblemDetails body that follows RFC 9457 and references the
            // relevant HTTP spec section so API consumers can look up the status semantics.
            var problem = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Authentication is required to access this resource.",
                Instance = ctx.HttpContext.Request.Path
            };

            return ctx.Response.WriteAsJsonAsync(problem);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handles the JWT Bearer forbidden event by returning a <c>403 Forbidden</c>
    ///     RFC 9457 ProblemDetails response when an authenticated user lacks the required permissions.
    /// </summary>
    /// <param name="ctx">The context supplied by the JWT Bearer middleware when access is forbidden.</param>
    private static Task OnForbidden(ForbiddenContext ctx)
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        ctx.Response.ContentType = "application/problem+json";

        // Distinct RFC reference from the 401 handler: 403 means the identity is known
        // but the server is refusing access, not that authentication is missing.
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            Title = "Forbidden",
            Status = StatusCodes.Status403Forbidden,
            Detail = "You do not have permission to access this resource.",
            Instance = ctx.HttpContext.Request.Path
        };

        return ctx.Response.WriteAsJsonAsync(problem);
    }
}