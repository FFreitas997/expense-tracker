using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Common;
using Application.Common.Errors;
using Application.DTOs.Auth;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Email.Models;
using Infrastructure.Email.Queue;
using Infrastructure.Email.Templates;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services.Implementations;

public class AuthService(
    ILogger<AuthService> logger,
    IConfiguration config,
    UserManager<User> manager,
    IEmailQueue emailQueue
) : IAuthService
{
    private const string LoginProvider = "ExpenseTracker";
    private const string RefreshTokenName = "RefreshToken";

    // ── IAuthService ──────────────────────────────────────────────────────

    /// <summary>Registers a new user and returns access and refresh tokens on success.</summary>
    public async Task<Result<TokenResponseDto>> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default)
    {
        logger.LogInformation("Registration attempt for email: {Email}", dto.Email);

        var existing = await manager.FindByEmailAsync(dto.Email);

        if (existing is not null)
        {
            logger.LogWarning("Registration failed: email already in use - {Email}", dto.Email);
            return Error.User.EmailAlreadyInUse;
        }

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            UserName = dto.Email,
            Role = dto.Role,
            State = UserState.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = dto.Email
        };

        var createResult = await manager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
        {
            var error = createResult.Errors.FirstOrDefault();
            logger.LogWarning("User creation failed for {Email}: {Error}", dto.Email, error?.Description);
            return Error.User.RegistrationFailed(error?.Description);
        }

        var roleResult = await manager.AddToRoleAsync(user, dto.Role);
        if (!roleResult.Succeeded)
        {
            logger.LogWarning("Role assignment failed for {Email}, rolling back user creation", dto.Email);
            await manager.DeleteAsync(user);
            return Error.User.RoleAssignmentFailed;
        }

        logger.LogInformation("User registered successfully: {Email} with role {Role}", dto.Email, dto.Role);

        var tokens = await IssueTokensAsync(user);
        return Result<TokenResponseDto>.Success(tokens);
    }

    /// <summary>Authenticates a user by email and password, returning tokens on success.</summary>
    public async Task<Result<TokenResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken ct = default)
    {
        logger.LogInformation("Login attempt for email: {Email}", dto.Email);

        var user = await manager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            logger.LogWarning("Login failed: user not found for email {Email}", dto.Email);
            return Error.Auth.InvalidCredentials;
        }

        if (await manager.IsLockedOutAsync(user))
        {
            logger.LogWarning("Login blocked: account locked out for user {UserId}", user.Id);
            return Error.Auth.TooManyRequests;
        }

        var validPassword = await manager.CheckPasswordAsync(user, dto.Password);
        if (!validPassword)
        {
            logger.LogWarning("Login failed: invalid password for user {UserId}", user.Id);
            await manager.AccessFailedAsync(user);
            return Error.Auth.InvalidCredentials;
        }

        await manager.ResetAccessFailedCountAsync(user);

        user.LastLogin = DateTime.UtcNow;
        await manager.UpdateAsync(user);

        logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        var tokens = await IssueTokensAsync(user);
        return Result<TokenResponseDto>.Success(tokens);
    }

    /// <summary>Issues a new token pair using a valid refresh token.</summary>
    public async Task<Result<TokenResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        logger.LogInformation("Token refresh attempt");

        try
        {
            var (userId, rawToken) = DecodeRefreshToken(refreshToken);

            var user = await manager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                logger.LogWarning("Token refresh failed: user {UserId} not found", userId);
                return Error.Auth.InvalidToken;
            }

            var storedToken = await manager.GetAuthenticationTokenAsync(user, LoginProvider, RefreshTokenName);
            if (storedToken is null || storedToken != rawToken)
            {
                logger.LogWarning("Token refresh failed: token mismatch for user {UserId}", userId);
                return Error.Auth.InvalidToken;
            }

            logger.LogInformation("Token refreshed successfully for user {UserId}", userId);

            var tokens = await IssueTokensAsync(user);
            return Result<TokenResponseDto>.Success(tokens);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Token refresh failed due to an unexpected error");
            return Error.Auth.InvalidToken;
        }
    }

    /// <summary>Invalidates the refresh token for the given session, effectively logging the user out.</summary>
    public async Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        logger.LogInformation("Logout attempt");

        try
        {
            var (userId, _) = DecodeRefreshToken(refreshToken);

            var user = await manager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                logger.LogWarning("Logout: user {UserId} not found, treating as already logged out", userId);
                return Result<bool>.Success(true);
            }

            await manager.RemoveAuthenticationTokenAsync(user, LoginProvider, RefreshTokenName);

            logger.LogInformation("User {UserId} logged out successfully", userId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Logout failed due to an unexpected error");
            return Error.Auth.InvalidToken;
        }
    }

    /// <summary>Initiates the password reset flow by generating a reset token for the given email address.</summary>
    public async Task<Result<bool>> ForgotPasswordAsync(string email, CancellationToken ct = default)
    {
        logger.LogInformation("Forgot password request for email: {Email}", email);

        var user = await manager.FindByEmailAsync(email);

        // Do not reveal whether the user exists
        if (user is null)
        {
            logger.LogDebug("Forgot password: no account found for {Email}, returning success to avoid enumeration",
                email);
            return Result<bool>.Success(true);
        }

        // Identity generates a secure token
        var token = await manager.GeneratePasswordResetTokenAsync(user);
        var resetLink = BuildResetLink(token, email);

        var emailMessage = new EmailMessage
        {
            To = user.Email!,
            ToName = user.FullName,
            Subject = "Reset your Expense Tracker password",
            Body = PasswordResetTemplate.Build(user.FullName, resetLink),
            IsHtml = true
        };

        // Enqueue — does NOT block the request thread ✅
        await emailQueue.EnqueueAsync(emailMessage, ct);

        logger.LogInformation("Password reset token generated for user {UserId}", user.Id);

        return Result<bool>.Success(true);
    }

    /// <summary>Resets the user's password using a previously issued reset token and invalidates active refresh tokens.</summary>
    public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequestDto dto, CancellationToken ct = default)
    {
        logger.LogInformation("Password reset attempt for email: {Email}", dto.Email);

        var user = await manager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            logger.LogWarning("Password reset failed: user not found for email {Email}", dto.Email);
            return Error.Auth.InvalidToken;
        }

        var result = await manager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        if (!result.Succeeded)
        {
            logger.LogWarning("Password reset failed for user {UserId}: invalid or expired token", user.Id);
            return Error.Auth.InvalidToken;
        }

        // Invalidate any active refresh token after a password reset
        await manager.RemoveAuthenticationTokenAsync(user, LoginProvider, RefreshTokenName);

        logger.LogInformation("Password reset successfully for user {UserId}", user.Id);

        return Result<bool>.Success(true);
    }

    // ── Token generation ──────────────────────────────────────────────────

    /// <summary>Builds and signs a JWT access token for the given user and their roles.</summary>
    private string GenerateJwt(User user, IList<string> roles)
    {
        logger.LogDebug("Generating JWT for user {UserId} with roles: {Roles}", user.Id, string.Join(", ", roles));

        var jwt = config.GetSection("Jwt");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        if (roles.Contains("Admin"))
            claims.Add(new Claim("backoffice-access", "true"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expirationMinutes = jwt.GetValue("ExpirationMinutes", 60);

        var token = new JwtSecurityToken(
            jwt["Issuer"],
            jwt["Audience"],
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(expirationMinutes),
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Generates a cryptographically secure random refresh token.</summary>
    private static string GenerateRawRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    /// <summary>Encodes a user ID and raw refresh token into a single Base64 string for transport.</summary>
    private static string EncodeRefreshToken(Guid userId, string rawToken)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userId}:{rawToken}"));
    }

    /// <summary>Decodes a Base64-encoded refresh token back into its user ID and raw token components.</summary>
    private static (Guid userId, string rawToken) DecodeRefreshToken(string refreshToken)
    {
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(refreshToken));
        var separatorIndex = decoded.IndexOf(':');
        return (Guid.Parse(decoded[..separatorIndex]), decoded[(separatorIndex + 1)..]);
    }

    /// <summary>Generates a new access token and refresh token pair, persisting the refresh token for the user.</summary>
    private async Task<TokenResponseDto> IssueTokensAsync(User user)
    {
        logger.LogDebug("Issuing tokens for user {UserId}", user.Id);

        var roles = await manager.GetRolesAsync(user);
        var accessToken = GenerateJwt(user, roles);

        var rawRefreshToken = GenerateRawRefreshToken();
        await manager.SetAuthenticationTokenAsync(user, LoginProvider, RefreshTokenName, rawRefreshToken);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = EncodeRefreshToken(user.Id, rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddMinutes(config.GetValue("Jwt:ExpirationMinutes", 60))
        };
    }

    private string BuildResetLink(string token, string email)
    {
        var appUrl = config.GetSection("AppSettings")["FrontendBaseUrl"];

        if (appUrl == null)
        {
            logger.LogError("FrontendBaseUrl is not configured in AppSettings");
            throw new InvalidOperationException("FrontendBaseUrl is not configured");
        }

        var encodedToken = Uri.EscapeDataString(token);
        var encodedEmail = Uri.EscapeDataString(email);
        var baseUrl = appUrl;

        return $"{baseUrl}/reset-password?token={encodedToken}&email={encodedEmail}";
    }
}