using API.Exceptions.Handlers;

namespace API.Extensions;

public static class ExceptionExtensions
{
    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        // Order matters — most specific first, fallback last
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<AppExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                ctx.ProblemDetails.Extensions["traceId"] =
                    ctx.HttpContext.TraceIdentifier;

                ctx.ProblemDetails.Extensions["timestamp"] =
                    DateTime.UtcNow.ToString("O");
            };
        });

        return services;
    }
}