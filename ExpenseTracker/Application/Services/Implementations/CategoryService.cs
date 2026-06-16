using Application.Common;
using Application.DTOs.Category;
using Application.Services.Interfaces;
using Infrastructure.Cache.Interfaces;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

// result pattern
// custom exceptions
// caching
// logging
// validation (FluentValidation)
// unit of work pattern
public class CategoryService(
    ILogger<CategoryService> logger,
    IUnitOfWork unit,
    ICacheRepository cache
) : ICategoryService
{
    private const string CacheKeyAll = "categories:all";

    public async Task<Result<IEnumerable<CategoryResponseDto>>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<CategoryResponseDto>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<CategoryResponseDto>> CreateCustomAsync(CategoryCreateDto dto, Guid userId,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<CategoryResponseDto>> UpdateCustomAsync(Guid id, CategoryUpdateDto dto, Guid userId,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<bool>> DeleteCustomAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<IEnumerable<CategoryResponseDto>>> GetAllSystemAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<CategoryResponseDto>> CreateSystemAsync(CategoryCreateDto dto,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<CategoryResponseDto>> UpdateSystemAsync(Guid id, CategoryUpdateDto dto,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<bool>> DeleteSystemAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    private static string CacheKey(Guid id)
    {
        return $"category:{id}";
    }
}