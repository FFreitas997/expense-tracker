using Microsoft.AspNetCore.Http;

namespace Application.Exceptions.Custom;

public sealed class ForbiddenException(string message = "You do not have permission to perform this action.")
    : AppException(message, StatusCodes.Status403Forbidden);