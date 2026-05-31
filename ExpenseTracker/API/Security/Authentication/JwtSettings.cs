using System.ComponentModel.DataAnnotations;

namespace API.Security.Authentication;

/// <summary>
/// Strongly-typed settings bound from the <c>Jwt</c> configuration section,
/// used to configure JWT Bearer token generation and validation.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>
    /// The symmetric secret key used to sign and verify JWT tokens.
    /// Must be at least 32 characters to provide adequate HMAC-SHA256 entropy.
    /// </summary>
    [Required, MinLength(32)]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// The expected <c>iss</c> (issuer) claim value written into generated tokens
    /// and validated on every incoming token.
    /// </summary>
    [Required, MinLength(1)]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// The expected <c>aud</c> (audience) claim value written into generated tokens
    /// and validated on every incoming token.
    /// </summary>
    [Required, MinLength(1)]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Lifetime of an access token in minutes. Defaults to <c>60</c>.
    /// Must be between 1 and 1440 (24 hours).
    /// </summary>
    [Range(1, 1440)]
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Lifetime of a refresh token in days. Defaults to <c>7</c>.
    /// Must be between 1 and 90 days.
    /// </summary>
    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 7;
}