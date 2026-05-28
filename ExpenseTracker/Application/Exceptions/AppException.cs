namespace Application.Exceptions;

// Application/Common/Exceptions/AppException.cs
public abstract class AppException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}