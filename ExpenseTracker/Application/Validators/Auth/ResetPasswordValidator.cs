using Application.DTOs.Auth;
using FluentValidation;
using Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace Application.Validators.Auth;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordValidator(IOptions<IdentitySettings> options)
    {
        var passwordSettings = options.Value.Password;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(passwordSettings.PasswordMinLength)
            .WithMessage($"Password must be at least {passwordSettings.PasswordMinLength} characters long.");

        if (passwordSettings.RequireDigit)
            RuleFor(x => x.NewPassword)
                .Matches(@"\d").WithMessage("Password must contain at least one digit.");

        if (passwordSettings.RequireUppercase)
            RuleFor(x => x.NewPassword)
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.");

        if (passwordSettings.RequireLowercase)
            RuleFor(x => x.NewPassword)
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.");

        if (passwordSettings.RequireNonAlphanumeric)
            RuleFor(x => x.NewPassword)
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");
    }
}