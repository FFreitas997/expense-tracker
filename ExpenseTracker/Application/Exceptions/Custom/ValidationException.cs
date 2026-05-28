using Microsoft.AspNetCore.Http;

namespace Application.Exceptions.Custom;

public sealed class ValidationException(IDictionary<string, string[]> errors)
    : AppException("One or more validation errors occurred.", StatusCodes.Status400BadRequest)
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}