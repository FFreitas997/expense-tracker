using Microsoft.AspNetCore.Http;

namespace Application.Exceptions.Custom;

public sealed class ConflictException(string resource, string detail)
    : AppException($"{resource} conflict: {detail}", StatusCodes.Status409Conflict);