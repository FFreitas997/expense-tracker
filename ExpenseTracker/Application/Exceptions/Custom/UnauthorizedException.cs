using Microsoft.AspNetCore.Http;

namespace Application.Exceptions.Custom;

public sealed class UnauthorizedException(string message = "Authentication is required.")
    : AppException(message, StatusCodes.Status401Unauthorized);