using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using ValidationException = Application.Exceptions.Custom.ValidationException;

namespace API.Filters;

/// <summary>
/// A global ASP.NET Core action filter that automatically validates all complex
/// action arguments using their registered FluentValidation <see cref="IValidator{T}"/>
/// before the action body executes.
/// </summary>
/// <remarks>
/// The filter is registered globally in <c>AddControllers</c> so it applies to
/// every controller action without requiring per-action attributes.
/// <para>
/// Validation is performed per argument:
/// <list type="bullet">
///   <item><description>
///     Primitive types, value types, <see cref="string"/>, and
///     <see cref="CancellationToken"/> are skipped — only complex DTO types are validated.
///   </description></item>
///   <item><description>
///     <see cref="IValidator{T}"/> is resolved dynamically from the DI container;
///     arguments with no registered validator are silently skipped.
///   </description></item>
///   <item><description>
///     Errors from all arguments are merged into a single dictionary before
///     throwing, so the client receives all field errors in one response.
///   </description></item>
/// </list>
/// When validation fails a <see cref="ValidationException"/> is thrown, which is
/// then caught and mapped to a <c>400 Bad Request</c> ProblemDetails response by
/// <c>ValidationExceptionHandler</c>.
/// </para>
/// </remarks>
/// <param name="provider">The <see cref="IServiceProvider"/> used to resolve validators from DI.</param>
/// <param name="logger">The logger used to record debug and warning entries during validation.</param>
public sealed class ValidationFilter(IServiceProvider provider, ILogger<ValidationFilter> logger) : IAsyncActionFilter
{
    /// <summary>
    /// Validates all eligible action arguments before executing the action.
    /// Throws a <see cref="ValidationException"/> if any argument fails validation.
    /// </summary>
    /// <param name="context">The action executing context, providing access to action arguments.</param>
    /// <param name="next">The delegate to invoke to execute the action if validation passes.</param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Accumulate errors from all arguments so the client receives a complete
        // picture of every validation failure in a single 400 response.
        var errors = new Dictionary<string, string[]>();

        foreach (var argument in context.ActionArguments.Values)
        {
            // Skip null arguments and types that do not require validation (e.g.
            // primitives, value types, strings) — see ShouldSkip for the full list.
            if (argument is null || ShouldSkip(argument.GetType()))
                continue;

            // Build the closed generic IValidator<T> type for the current argument
            // and attempt to resolve it from the DI container. This allows validators
            // to be registered once in the Application layer and discovered automatically.
            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            // If no validator is registered for this type, skip it silently.
            // Not every DTO needs a validator (e.g. simple query parameters).
            if (provider.GetService(validatorType) is not IValidator validator)
                continue;

            logger.LogDebug(
                "Validating {ArgumentType} on {Action}",
                argument.GetType().Name,
                context.ActionDescriptor.DisplayName);

            var validationContext = new ValidationContext<object>(argument);

            // Use the request's CancellationToken so validation is aborted if the
            // client disconnects before the response is sent.
            var result = await validator.ValidateAsync(
                validationContext,
                context.HttpContext.RequestAborted);

            if (result.IsValid) continue;

            // ── Merge errors ──────────────────────────────────
            // Multiple action arguments may produce errors for the same property name
            // (e.g. two DTOs both have an "Email" field), so existing entries are
            // extended rather than overwritten to preserve all failure messages.
            foreach (var failure in result.Errors)
                if (errors.TryGetValue(failure.PropertyName, out var existing))
                    errors[failure.PropertyName] = [.. existing, failure.ErrorMessage];
                else
                    errors[failure.PropertyName] = [failure.ErrorMessage];
        }

        if (errors.Count > 0)
        {
            // Log before throwing so the error count is visible in structured logs
            // even if the downstream exception handler swallows the stack trace.
            logger.LogWarning(
                "Validation failed for {Action} with {ErrorCount} error(s).",
                context.ActionDescriptor.DisplayName,
                errors.Count);

            // Throw ValidationException with the merged errors dictionary;
            // ValidationExceptionHandler maps this to a 400 ValidationProblemDetails response.
            throw new ValidationException(errors);
        }

        await next();
    }

    /// <summary>
    /// Returns <c>true</c> for types that should be excluded from FluentValidation,
    /// namely primitives, value types, <see cref="string"/>, and <see cref="CancellationToken"/>.
    /// Only complex reference types (DTOs) are considered eligible for validation.
    /// </summary>
    /// <param name="type">The argument type to evaluate.</param>
    private static bool ShouldSkip(Type type)
    {
        return type.IsPrimitive    ||   // int, bool, char, etc.
               type.IsValueType    ||   // structs, enums, DateTime, Guid, etc.
               type == typeof(string) ||
               type == typeof(CancellationToken);
    }
}