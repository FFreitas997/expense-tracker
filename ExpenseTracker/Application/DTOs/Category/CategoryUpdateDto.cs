namespace Application.DTOs.Category;

public sealed class CategoryUpdateDto
{
    public string Name { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public string Color { get; set; } = null!;
}