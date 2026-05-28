using Microsoft.AspNetCore.Http;

namespace Application.Exceptions.Custom;

public sealed class BusinessRuleException(string rule, string message)
    : AppException(message, StatusCodes.Status422UnprocessableEntity)
{
    public string Rule { get; } = rule;
}