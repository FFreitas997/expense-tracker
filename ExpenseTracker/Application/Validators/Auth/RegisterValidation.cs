using Application.DTOs.Auth;
using FluentValidation;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Application.Validators.Auth;

public class RegisterValidation : AbstractValidator<RegisterRequestDto>
{
    public RegisterValidation(IOptions<IdentitySettings> options)
    {
        var passwordSettings = options.Value.Password;
        var userSettings = options.Value.User;

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(passwordSettings.PasswordMinLength)
                .WithMessage($"Password must be at least {passwordSettings.PasswordMinLength} characters long.");

        if (passwordSettings.RequireDigit)
            RuleFor(x => x.Password)
                .Matches(@"\d").WithMessage("Password must contain at least one digit.");

        if (passwordSettings.RequireUppercase)
            RuleFor(x => x.Password)
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.");

        if (passwordSettings.RequireLowercase)
            RuleFor(x => x.Password)
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.");

        if (passwordSettings.RequireNonAlphanumeric)
            RuleFor(x => x.Password)
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => role is "Admin" or "User")
                .WithMessage("Role must be either 'Admin' or 'User'.");
    }
}
