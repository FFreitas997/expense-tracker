using Application.Common;
using Application.DTOs.Category;

namespace Application.Services.Interfaces;

public interface ICategoryService
{
    // ── Front-Office ─────────────────────────────────────────

    Task<Result<IEnumerable<CategoryResponseDto>>> GetAllAsync(Guid userId, CancellationToken ct = default);

    Task<Result<CategoryResponseDto>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);

    Task<Result<CategoryResponseDto>> CreateCustomAsync(CategoryCreateDto dto, Guid userId,
        CancellationToken ct = default);

    Task<Result<CategoryResponseDto>> UpdateCustomAsync(Guid id, CategoryUpdateDto dto, Guid userId,
        CancellationToken ct = default);

    Task<Result<bool>> DeleteCustomAsync(Guid id, Guid userId, CancellationToken ct = default);

    // ── Back-Office ──────────────────────────────────────────

    Task<Result<IEnumerable<CategoryResponseDto>>> GetAllSystemAsync(CancellationToken ct = default);

    Task<Result<CategoryResponseDto>> CreateSystemAsync(CategoryCreateDto dto, CancellationToken ct = default);

    Task<Result<CategoryResponseDto>> UpdateSystemAsync(Guid id, CategoryUpdateDto dto, CancellationToken ct = default);

    Task<Result<bool>> DeleteSystemAsync(Guid id, CancellationToken ct = default);
}