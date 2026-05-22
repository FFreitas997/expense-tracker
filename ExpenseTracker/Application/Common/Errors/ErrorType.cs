namespace Application.Common.Errors;

public enum ErrorType
{
    None = 200,
    Validation = 400,
    NotFound = 404,
    Unauthorized = 401,
    Forbidden = 403,
    Conflict = 409,
    TooManyRequests = 429,
    InternalServerError = 500
}