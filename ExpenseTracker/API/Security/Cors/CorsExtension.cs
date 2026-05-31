namespace API.Security.Cors;

/// <summary>
///     Extension methods for registering and configuring the application's
///     Cross-Origin Resource Sharing (CORS) policy on the <see cref="IServiceCollection" />.
/// </summary>
public static class CorsExtension
{
    /// <summary>The name of the CORS policy registered and applied across the application.</summary>
    public const string CorsPolicyName = "CorsPolicy";

    /// <summary>
    ///     Adds a named CORS policy built from the <c>Cors</c> configuration section.
    /// </summary>
    /// <remarks>
    ///     The method performs eager validation of the configuration before building the policy
    ///     so that any misconfiguration surfaces at startup rather than at runtime:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>All three allow-lists (origins, methods, headers) must be non-empty.</description>
    ///         </item>
    ///         <item>
    ///             <description>Wildcard origins and credentials cannot be combined (browser restriction).</description>
    ///         </item>
    ///         <item>
    ///             <description>Plain HTTP origins are rejected outside the Development environment.</description>
    ///         </item>
    ///     </list>
    ///     The resulting policy explicitly enumerates allowed origins, methods, headers, and exposed
    ///     headers rather than using open wildcards, which minimises the CORS attack surface.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" /> to configure.</param>
    /// <param name="env">The host environment used to enforce HTTPS-only origins in non-development.</param>
    /// <param name="config">The application configuration used to bind <c>CorsSettings</c>.</param>
    /// <returns>The same <paramref name="services" /> instance to allow method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the <c>Cors</c> configuration section is missing, any allow-list is empty,
    ///     wildcard origins are combined with credentials, or plain HTTP origins are used outside Development.
    /// </exception>
    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        IHostEnvironment env,
        IConfiguration config
    )
    {
        // ── Configuration validation ───────────────────────────
        // Bind and validate eagerly so the application fails fast at startup
        // rather than serving incorrect CORS headers in production.
        var settings = config.GetSection("Cors").Get<CorsSettings>();

        if (settings is null)
            throw new InvalidOperationException("Cors settings are not configured properly.");

        // All three allow-lists are mandatory; an empty list would effectively
        // block all cross-origin requests, which is almost certainly a misconfiguration.
        if (settings.AllowedOrigins.Length == 0)
            throw new InvalidOperationException("Cors settings must specify at least one allowed origin.");

        if (settings.AllowedMethods.Length == 0)
            throw new InvalidOperationException("Cors settings must specify at least one allowed method.");

        if (settings.AllowedHeaders.Length == 0)
            throw new InvalidOperationException("Cors settings must specify at least one allowed header.");

        // The Fetch specification forbids credentials with a wildcard origin (*).
        // Allowing both would cause browsers to reject the preflight response.
        if (settings is { AllowWildcardOrigins: true, AllowCredentials: true })
            throw new InvalidOperationException(
                "Cors settings cannot combine AllowWildcardOrigins with AllowCredentials.");

        // Null or whitespace-only origin strings would be silently ignored by the
        // CORS middleware, indicating a configuration error that should be caught early.
        var invalidOrigins = settings.AllowedOrigins.Where(string.IsNullOrWhiteSpace).ToArray();
        if (invalidOrigins.Length > 0)
            throw new InvalidOperationException(
                $"Cors settings contain invalid origins: {string.Join(", ", invalidOrigins)}");

        // Plain HTTP origins are only permitted in Development (e.g. localhost tooling);
        // all other environments must use HTTPS to protect credentials and tokens in transit.
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
                // ── Allowed origins ───────────────────────────
                // Restrict access to the explicitly configured origins only, avoiding
                // the security risk of an open wildcard (*) in production.
                builder.WithOrigins(settings.AllowedOrigins);

                // ── Allowed methods ───────────────────────────
                // Permit only the HTTP verbs required by the API (e.g. GET, POST, PUT,
                // DELETE) to reduce the surface area for cross-origin abuse.
                builder.WithMethods(settings.AllowedMethods);

                // ── Allowed headers ───────────────────────────
                // Limit the request headers the browser is allowed to send, covering
                // authentication tokens and content negotiation headers used by the API.
                builder.WithHeaders(settings.AllowedHeaders);

                // ── Credentials ───────────────────────────────
                if (settings.AllowCredentials)
                    // Required when the client sends cookies, Authorization headers,
                    // or TLS client certificates in cross-origin requests.
                    builder.AllowCredentials();
                else
                    // Explicitly disallow credentials so the policy intent is unambiguous
                    // and browsers do not forward sensitive material unexpectedly.
                    builder.DisallowCredentials();

                // ── Wildcard subdomains ───────────────────────
                // When enabled, allows any subdomain of the listed origins (e.g.
                // app.example.com, api.example.com) without enumerating each one.
                if (settings.AllowWildcardOrigins)
                    builder.SetIsOriginAllowedToAllowWildcardSubdomains();

                // ── Exposed headers ───────────────────────────
                // Browser JavaScript can only read response headers that are explicitly
                // exposed here; the list covers file download, versioning, rate-limiting,
                // and token-expiry signals consumed by API clients.
                builder.WithExposedHeaders(
                    "Content-Disposition", // file download filename
                    "api-supported-versions", // advertised by Asp.Versioning
                    "api-deprecated-versions", // advertised by Asp.Versioning
                    "X-Token-Expired", // signals the access token has expired
                    "X-Rate-Limit-RetryAfter", // seconds until the rate-limit window resets
                    "X-Rate-Limit-Reset", // absolute UTC timestamp of the reset
                    "X-Token-Expired-At" // timestamp when the token expired
                );

                // ── Preflight cache ───────────────────────────
                // Instructs browsers to cache the preflight OPTIONS response for the
                // configured duration, reducing the number of extra round-trips for
                // repeat cross-origin requests from the same client.
                builder.SetPreflightMaxAge(TimeSpan.FromMinutes(settings.PreflightMaxAgeMinutes));
            });
        });

        return services;
    }
}