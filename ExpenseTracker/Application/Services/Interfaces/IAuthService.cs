using Application.Common;
using Application.DTOs.Auth;

namespace Application.Services.Interfaces;

public interface IAuthService
{
    Task<Result<TokenResponseDto>> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default);

    Task<Result<TokenResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken ct = default);

    Task<Result<TokenResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct = default);

    Task<Result<bool>> ForgotPasswordAsync(string email, CancellationToken ct = default);

    Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequestDto dto, CancellationToken ct = default);
}