using System.Diagnostics;
using API.Observability.Tracing;
using Application.DTOs.Auth;
using Application.Services.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.FrontOffice.V1;

/// <summary>
/// Handles all front-office authentication operations: registration, login,
/// token refresh, logout, and password recovery.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/frontoffice/auth")]
public class AuthController(IAuthService service) : ControllerBase
{
    /// <summary>
    /// Registers a new user account and returns an initial access/refresh token pair.
    /// </summary>
    /// <param name="dto">Registration payload containing full name, e-mail, password and role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 Created with <see cref="TokenResponseDto"/> on success; problem details on failure.</returns>
    [HttpPost("register")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto, CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance.StartActivity("AuthController.Register");

        var result = await service.RegisterAsync(dto, ct);

        if (result.IsFailure)
        {
            var error = result.Error;
            activity?.SetTag("result.status", "failure");
            activity?.SetTag("error.code", error.Code);
            activity?.SetTag("error.details", error.Details);
            activity?.SetStatus(ActivityStatusCode.Error, error.Details);
        }
        else
        {
            activity?.SetTag("result.status", "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        return result.Match<IActionResult>(
            token => Created(string.Empty, token),
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type
            ));
    }

    /// <summary>
    /// Authenticates an existing user by e-mail and password.
    /// </summary>
    /// <param name="dto">Login payload containing e-mail and password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with <see cref="TokenResponseDto"/> on success; problem details on failure.</returns>
    [HttpPost("login")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance.StartActivity("AuthController.Login");

        var result = await service.LoginAsync(dto, ct);

        if (result.IsFailure)
        {
            var error = result.Error;
            activity?.SetTag("result.status", "failure");
            activity?.SetTag("error.code", error.Code);
            activity?.SetTag("error.details", error.Details);
            activity?.SetStatus(ActivityStatusCode.Error, error.Details);
        }
        else
        {
            activity?.SetTag("result.status", "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        return result.Match<IActionResult>(
            token => Ok(token),
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type
            ));
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new access/refresh token pair.
    /// The submitted refresh token is invalidated after a successful exchange.
    /// </summary>
    /// <param name="dto">Payload containing the current refresh token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with <see cref="TokenResponseDto"/> on success; problem details on failure.</returns>
    [HttpPost("refresh-token")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto, CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance.StartActivity("AuthController.RefreshToken");

        var result = await service.RefreshTokenAsync(dto.RefreshToken, ct);

        if (result.IsFailure)
        {
            var error = result.Error;
            activity?.SetTag("result.status", "failure");
            activity?.SetTag("error.code", error.Code);
            activity?.SetTag("error.details", error.Details);
            activity?.SetStatus(ActivityStatusCode.Error, error.Details);
        }
        else
        {
            activity?.SetTag("result.status", "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        return result.Match<IActionResult>(
            token => Ok(token),
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type
            ));
    }

    /// <summary>
    /// Invalidates the given refresh token, ending the user's session.
    /// Intentionally accepts requests with an expired access token so that
    /// users can always log out regardless of JWT lifetime.
    /// </summary>
    /// <param name="dto">Payload containing the refresh token to revoke.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success; problem details on failure.</returns>
    [HttpPost("logout")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto, CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance.StartActivity("AuthController.Logout");

        var result = await service.LogoutAsync(dto.RefreshToken, ct);

        if (result.IsFailure)
        {
            var error = result.Error;
            activity?.SetTag("result.status", "failure");
            activity?.SetTag("error.code", error.Code);
            activity?.SetTag("error.details", error.Details);
            activity?.SetStatus(ActivityStatusCode.Error, error.Details);
        }
        else
        {
            activity?.SetTag("result.status", "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        return result.Match<IActionResult>(
            _ => NoContent(),
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type
            ));
    }

    /// <summary>
    /// Initiates the password-reset flow by sending a reset link to the supplied e-mail address.
    /// Always returns 204 regardless of whether an account exists, to prevent user enumeration.
    /// </summary>
    /// <param name="dto">Payload containing the account e-mail address.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success; problem details on failure.</returns>
    [HttpPost("forgot-password")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto, CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance.StartActivity("AuthController.ForgotPassword");

        var result = await service.ForgotPasswordAsync(dto.Email, ct);

        if (result.IsFailure)
        {
            var error = result.Error;
            activity?.SetTag("result.status", "failure");
            activity?.SetTag("error.code", error.Code);
            activity?.SetTag("error.details", error.Details);
            activity?.SetStatus(ActivityStatusCode.Error, error.Details);
        }
        else
        {
            activity?.SetTag("result.status", "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        return result.Match<IActionResult>(
            _ => NoContent(),
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type
            ));
    }

    /// <summary>
    /// Completes the password-reset flow by applying the new password with the token
    /// delivered via e-mail. All active refresh tokens are revoked on success,
    /// requiring the user to log in again.
    /// </summary>
    /// <param name="dto">Payload containing the e-mail, reset token and new password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success; problem details on failure.</returns>
    [HttpPost("reset-password")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto, CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance.StartActivity("AuthController.ResetPassword");

        var result = await service.ResetPasswordAsync(dto, ct);

        if (result.IsFailure)
        {
            var error = result.Error;
            activity?.SetTag("result.status", "failure");
            activity?.SetTag("error.code", error.Code);
            activity?.SetTag("error.details", error.Details);
            activity?.SetStatus(ActivityStatusCode.Error, error.Details);
        }
        else
        {
            activity?.SetTag("result.status", "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        return result.Match<IActionResult>(
            _ => NoContent(),
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type
            ));
    }
}