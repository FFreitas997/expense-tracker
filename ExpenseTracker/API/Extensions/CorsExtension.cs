using API.Settings;

namespace API.Extensions;

public static class CorsExtension
{
    public const string CorsPolicyName = "CorsPolicy";

    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        IHostEnvironment env,
        IConfiguration config
    )
    {
        var settings = config.GetSection("Cors").Get<CorsSettings>();

        if (settings is null)
            throw new InvalidOperationException("Cors settings are not configured properly.");

        if (settings.AllowedOrigins.Length == 0)
            throw new InvalidOperationException("Cors settings must specify at least one allowed origin.");

        if (settings.AllowedMethods.Length == 0)
            throw new InvalidOperationException("Cors settings must specify at least one allowed method.");

        if (settings.AllowedHeaders.Length == 0)
            throw new InvalidOperationException("Cors settings must specify at least one allowed header.");

        if (settings is { AllowWildcardOrigins: true, AllowCredentials: true })
            throw new InvalidOperationException(
                "Cors settings cannot combine AllowWildcardOrigins with AllowCredentials.");

        var invalidOrigins = settings.AllowedOrigins.Where(string.IsNullOrWhiteSpace).ToArray();
        if (invalidOrigins.Length > 0)
            throw new InvalidOperationException(
                $"Cors settings contain invalid origins: {string.Join(", ", invalidOrigins)}");

        if (!env.IsDevelopment())
        {
            var insecureOrigins = settings.AllowedOrigins
                .Where(o => o.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (insecureOrigins.Length > 0)
                throw new InvalidOperationException(
                    $"Cors settings contain non-HTTPS origins in a non-development environment: {string.Join(", ", insecureOrigins)}");
        }

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, builder =>
            {
                // Allow specific origins to control which domains can access the API, enhancing security by not allowing all origins
                builder.WithOrigins(settings.AllowedOrigins);

                // Allow specific HTTP methods to ensure that only intended operations are permitted, enhancing security by not allowing all methods
                builder.WithMethods(settings.AllowedMethods);

                // Allow specific headers to enable necessary functionality while maintaining security by not allowing all headers
                builder.WithHeaders(settings.AllowedHeaders);

                if (settings.AllowCredentials)
                    builder.AllowCredentials(); // required for allowing cookies, authorization headers, or TLS client certificates in cross-origin requests
                else
                    builder.DisallowCredentials(); // explicitly disallow credentials to prevent security issues

                // If wildcard origins are allowed, set the policy to allow subdomains of the specified origins
                if (settings.AllowWildcardOrigins)
                    builder.SetIsOriginAllowedToAllowWildcardSubdomains();

                // Expose specific headers to the client to allow access to important information such as content disposition for file downloads and API versioning details
                builder.WithExposedHeaders(
                    "Content-Disposition",
                    "api-supported-versions",
                    "api-deprecated-versions",
                    "X-Token-Expired",
                    "X-Rate-Limit-RetryAfter",
                    "X-Rate-Limit-Reset",
                    "X-Token-Expired-At"
                );

                // Set preflight cache duration to reduce the number of preflight requests for better performance
                builder.SetPreflightMaxAge(TimeSpan.FromMinutes(settings.PreflightMaxAgeMinutes));
            });
        });

        return services;
    }
}