using Microsoft.AspNetCore.Http;

namespace Application.Exceptions.Custom;

public sealed class NotFoundException : AppException
{
    public NotFoundException(string resource, object key)
        : base($"{resource} with id '{key}' was not found.", StatusCodes.Status404NotFound)
    {
    }

    public NotFoundException(string message)
        : base(message, StatusCodes.Status404NotFound)
    {
    }
}