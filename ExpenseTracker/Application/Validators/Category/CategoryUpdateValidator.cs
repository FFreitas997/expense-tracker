using Application.DTOs.Category;
using FluentValidation;

namespace Application.Validators.Category;

public sealed class CategoryUpdateValidator : AbstractValidator<CategoryUpdateDto>
{
    public CategoryUpdateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Icon)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.Color)
            .NotEmpty()
            .Matches("^#([A-Fa-f0-9]{6})$")
            .WithMessage("Color must be a valid hex color (e.g. #FF6B6B).");
    }
}