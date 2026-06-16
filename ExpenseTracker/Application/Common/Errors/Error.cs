namespace Application.Common.Errors;

public sealed record Error(string Code, string Details, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    // category-specific errors
    public static class Category
    {
        public static readonly Error InvalidName =
            new("category.invalid_name", "Category name cannot be empty.", ErrorType.Validation);

        public static readonly Error InvalidIcon =
            new("category.invalid_icon", "Category icon cannot be empty.", ErrorType.Validation);

        public static readonly Error NameAlreadyInUse =
            new("category.name_already_in_use", "A category with the same name already exists.", ErrorType.Conflict);

        public static readonly Error CannotDeleteDefault =
            new("category.cannot_delete_default", "Default categories cannot be deleted.", ErrorType.Forbidden);

        public static Error NotFound(Guid id)
        {
            return new Error("category.not_found", $"Category with id {id} was not found.", ErrorType.NotFound);
        }
    }

    // expense-specific errors
    public static class Expense
    {
        public static readonly Error InvalidAmount =
            new("expense.invalid_amount", "Amount must be greater than zero.", ErrorType.Validation);

        public static readonly Error InvalidDate =
            new("expense.invalid_date", "Date must be in the past.", ErrorType.Validation);

        public static readonly Error InvalidDescription =
            new("expense.invalid_description", "Description cannot be empty.", ErrorType.Validation);

        public static Error NotFound(Guid id)
        {
            return new Error("expense.not_found", $"Expense with id {id} was not found.", ErrorType.NotFound);
        }
    }

    // budget-specific errors
    public static class Budget
    {
        public static readonly Error InvalidLimitAmount =
            new("budget.invalid_limit_amount", "Limit amount must be greater than zero.", ErrorType.Validation);

        public static readonly Error InvalidStartDate =
            new("budget.invalid_start_date", "Start date cannot be in the past.", ErrorType.Validation);

        public static readonly Error LimitExceeded =
            new("budget.limit_exceeded", "The budget limit has been exceeded.", ErrorType.Validation);

        public static Error NotFound(Guid id)
        {
            return new Error("budget.not_found", $"Budget with id {id} was not found.", ErrorType.NotFound);
        }
    }

    // user-specific errors
    public static class User
    {
        public static readonly Error InvalidEmail =
            new("user.invalid_email", "The provided email address is not valid.", ErrorType.Validation);

        public static readonly Error InvalidPassword =
            new("user.invalid_password", "Password does not meet the required criteria.", ErrorType.Validation);

        public static readonly Error EmailAlreadyInUse =
            new("user.email_already_in_use", "The provided email address is already in use.", ErrorType.Conflict);

        public static readonly Error RoleAssignmentFailed =
            new("auth.role_assignment_failed", "Failed to assign the specified role.", ErrorType.InternalServerError);

        public static Error RegistrationFailed(string? error)
        {
            return new Error("auth.registration_failed", error ?? "Registration failed.", ErrorType.Validation);
        }

        public static Error NotFound(Guid id)
        {
            return new Error("user.not_found", $"User with id {id} was not found.", ErrorType.NotFound);
        }
    }

    // authentication errors
    public static class Auth
    {
        public static readonly Error InvalidCredentials =
            new("auth.invalid_credentials", "The provided credentials are invalid.", ErrorType.Unauthorized);

        public static readonly Error TokenExpired =
            new("auth.token_expired", "The authentication token has expired.", ErrorType.Unauthorized);

        public static readonly Error InvalidToken =
            new("auth.invalid_token", "The authentication token is invalid.", ErrorType.Unauthorized);

        public static readonly Error TooManyRequests =
            new("auth.too_many_requests", "Too many authentication attempts. Please try again later.",
                ErrorType.TooManyRequests);
    }

    // generic errors
    public static class General
    {
        public static readonly Error InternalServerError =
            new("general.internal_server_error", "An unexpected error occurred. Please try again later.",
                ErrorType.InternalServerError);

        public static readonly Error Unauthorized =
            new("general.unauthorized", "You are not authorized to perform this action.", ErrorType.Unauthorized);

        public static readonly Error Forbidden =
            new("general.forbidden", "You do not have permission to access this resource.", ErrorType.Forbidden);
    }
}