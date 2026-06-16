namespace Application.DTOs.Category;

public sealed class CategoryResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public string Color { get; set; } = null!;
    public bool IsDefault { get; set; }
    public Guid? UserId { get; set; }
}