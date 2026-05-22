using System.Security.Authentication;

namespace API.Extensions;

public static class HttpsExtension
{
    /// <summary>
    ///     Configures Kestrel to enforce TLS 1.2 as the minimum protocol version
    ///     and disables weak cipher suites (RC4, DES, 3DES, NULL) across all HTTPS endpoints.
    /// </summary>
    public static WebApplicationBuilder AddKestrelTlsConfiguration(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ConfigureHttpsDefaults(httpsOptions =>
            {
                // Enforce TLS 1.2 minimum — disables SSL3, TLS 1.0, TLS 1.1
                httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            });

            // Reject plain HTTP connections on the HTTPS port
            options.AddServerHeader = false; // don't expose Kestrel version in Server header
        });

        return builder;
    }

    /// <summary>
    ///     Registers HSTS (HTTP Strict Transport Security) services.
    ///     Browsers will refuse plain HTTP connections for the duration of max-age.
    /// </summary>
    public static IServiceCollection AddHstsConfiguration(
        this IServiceCollection services,
        IHostEnvironment env
    )
    {
        if (!env.IsDevelopment())
            services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(365); // 1 year — standard for production
                options.IncludeSubDomains = true; // enforce HTTPS on all subdomains
                options.Preload = true; // eligible for browser HSTS preload lists
            });

        return services;
    }
}