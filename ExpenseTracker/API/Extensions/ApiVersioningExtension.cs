using Asp.Versioning;

namespace API.Extensions;

/// <summary>
/// Extension methods for registering and configuring API versioning
/// on the <see cref="IServiceCollection"/>.
/// </summary>
public static class ApiVersioningExtension
{
    /// <summary>
    /// Adds API versioning and API Explorer support with a configurable default version.
    /// </summary>
    /// <remarks>
    /// The configuration is split into two phases:
    /// <list type="number">
    ///   <item><description>
    ///     <b>Versioning</b> — sets the default version, enables graceful fallback for
    ///     unversioned requests, and supports three parallel version-reading strategies
    ///     (URL segment, query string, and custom header) so different client types can
    ///     each use the convention that suits them.
    ///   </description></item>
    ///   <item><description>
    ///     <b>API Explorer</b> — formats group names and substitutes the version token
    ///     in URL templates so OpenAPI document generators (e.g. Swashbuckle, Scalar)
    ///     produce correct, version-specific endpoint documentation.
    ///   </description></item>
    /// </list>
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="defaultMajorVersion">
    /// The major component of the default API version. Must be a non-negative integer.
    /// Defaults to <c>1</c>.
    /// </param>
    /// <param name="defaultMinorVersion">
    /// The minor component of the default API version. Must be a non-negative integer.
    /// Defaults to <c>0</c>.
    /// </param>
    /// <returns>The same <paramref name="services"/> instance to allow method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="defaultMajorVersion"/> or <paramref name="defaultMinorVersion"/>
    /// is negative.
    /// </exception>
    public static IServiceCollection AddApiVersioningConfiguration(
        this IServiceCollection services,
        int defaultMajorVersion = 1,
        int defaultMinorVersion = 0
    )
    {
        // Guard against negative version components; API versions must be non-negative
        // integers to form a valid semantic version string (e.g. "1.0").
        if (defaultMajorVersion < 0)
            throw new ArgumentOutOfRangeException(nameof(defaultMajorVersion),
                "Default major version must be a non-negative integer.");

        if (defaultMinorVersion < 0)
            throw new ArgumentOutOfRangeException(nameof(defaultMinorVersion),
                "Default minor version must be a non-negative integer.");

        services.AddApiVersioning(options =>
        {
            // ── Default version ───────────────────────────────
            // Constructs the default version from the supplied major/minor components
            // (e.g. major=1, minor=0 → ApiVersion "1.0").
            options.DefaultApiVersion = new ApiVersion(defaultMajorVersion, defaultMinorVersion);

            // Fall back to the default version when the client omits a version indicator,
            // ensuring backward compatibility for consumers that predate versioning.
            options.AssumeDefaultVersionWhenUnspecified = true;

            // ── Version advertisement ─────────────────────────
            // Adds api-supported-versions and api-deprecated-versions response headers
            // so clients can discover which versions are available without consulting docs.
            options.ReportApiVersions = true;

            // ── Version readers ───────────────────────────────
            // Combine multiple readers so clients can specify the version via whichever
            // convention best fits their use case; the first match wins.
            options.ApiVersionReader = ApiVersionReader.Combine(
                // URL segment reader: /api/v1.0/resource
                new UrlSegmentApiVersionReader(),

                // Query string reader: /api/resource?api-version=1.0
                new QueryStringApiVersionReader("api-version"),

                // Custom header reader: X-API-Version: 1.0
                new HeaderApiVersionReader("X-API-Version")
            );
        }).AddApiExplorer(options =>
        {
            // ── Group name format ─────────────────────────────
            // The 'v'VVV format produces names like "v1.0", which OpenAPI generators
            // use to create a separate document per API version.
            options.GroupNameFormat = "'v'VVV";

            // Replace the {version} token in route templates with the resolved version
            // string so generated OpenAPI paths are accurate (e.g. /api/v1.0/resource).
            options.SubstituteApiVersionInUrl = true;

            // Include version parameters in the documentation for version-neutral endpoints
            // (those decorated with [ApiVersionNeutral]) to signal they accept any version.
            options.AddApiVersionParametersWhenVersionNeutral = true;
        });

        return services;
    }
}