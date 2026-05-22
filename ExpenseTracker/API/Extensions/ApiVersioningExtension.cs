using Asp.Versioning;

namespace API.Extensions;

public static class ApiVersioningExtension
{
    public static IServiceCollection AddApiVersioningConfiguration(
        this IServiceCollection services,
        int defaultMajorVersion = 1,
        int defaultMinorVersion = 0
    )
    {
        if (defaultMajorVersion < 0)
            throw new ArgumentOutOfRangeException(nameof(defaultMajorVersion),
                "Default major version must be a non-negative integer.");

        if (defaultMinorVersion < 0)
            throw new ArgumentOutOfRangeException(nameof(defaultMinorVersion),
                "Default minor version must be a non-negative integer.");

        services.AddApiVersioning(options =>
        {
            // Specify the default API version (e.g., 1.0) and assume it when the client does not specify one
            options.DefaultApiVersion = new ApiVersion(defaultMajorVersion, defaultMinorVersion);

            // Assume the default version when the client does not specify one
            options.AssumeDefaultVersionWhenUnspecified = true;

            // Advertise the supported API versions in the response headers (api-supported-versions & api-deprecated-versions)
            options.ReportApiVersions = true;

            options.ApiVersionReader = ApiVersionReader.Combine(
                // Read version from URL segment (e.g., /v1.0/resource)
                new UrlSegmentApiVersionReader(),

                // Read version from query string (e.g., ?api-version=1.0)
                new QueryStringApiVersionReader("api-version"),

                // Read version from custom header (e.g., X-API-Version: 1.0)
                new HeaderApiVersionReader("X-API-Version")
            );
        }).AddApiExplorer(options =>
        {
            // Format the API version group name (e.g., "v1.0") and substitute it in the URL when generating API documentation
            options.GroupNameFormat = "'v'VVV";

            // Substitute the API version in the URL when generating API documentation (e.g., /v1.0/resource)
            options.SubstituteApiVersionInUrl = true;

            // Add API version parameters to the documentation for version-neutral endpoints (e.g., /resource) to indicate that they support all versions
            options.AddApiVersionParametersWhenVersionNeutral = true;
        });

        return services;
    }
}