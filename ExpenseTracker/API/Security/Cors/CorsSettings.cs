using System.ComponentModel.DataAnnotations;

namespace API.Security.Cors;

public class CorsSettings
{
    // List of allowed origins (e.g., "https://example.com", "http://localhost:3000")
    [Required] [MinLength(1)] public string[] AllowedOrigins { get; set; } = [];

    // List of allowed HTTP methods (e.g., "GET", "POST", "PUT", "DELETE")
    [Required] [MinLength(1)] public string[] AllowedMethods { get; set; } = [];

    // List of allowed headers (e.g., "Content-Type", "Authorization")
    [Required] [MinLength(1)] public string[] AllowedHeaders { get; set; } = [];

    // Indicates whether to allow credentials (e.g., cookies, authorization headers) in cross-origin requests
    public bool AllowCredentials { get; set; } = false;

    public bool AllowWildcardOrigins { get; set; } = false;

    [Range(1, 120)] public int PreflightMaxAgeMinutes { get; set; } = 10;
}