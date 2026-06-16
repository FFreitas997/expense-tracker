using Application.DTOs.Category;
using FluentValidation;

namespace Application.Validators.Category;

public sealed class CategoryCreateValidator : AbstractValidator<CategoryCreateDto>
{
    public CategoryCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Category name is required and cannot exceed 50 characters.");

        RuleFor(x => x.Icon)
            .NotEmpty()
            .MaximumLength(10)
            .WithMessage("Icon is required.");

        RuleFor(x => x.Color)
            .NotEmpty()
            .Matches("^#([A-Fa-f0-9]{6})$")
            .WithMessage("Color must be a valid hex color (e.g. #FF6B6B).");
    }
}