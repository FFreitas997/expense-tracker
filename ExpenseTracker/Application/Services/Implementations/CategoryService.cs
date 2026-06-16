using System.Data;
using Application.Common;
using Application.Common.Errors;
using Application.DTOs.Category;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Cache.Interfaces;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

/// <summary>
///     Implements <see cref="ICategoryService" /> providing CRUD operations for both
///     user-defined (custom) and system (default) categories.
///     Reads are served through a cache-aside layer; writes open explicit database
///     transactions at an appropriate isolation level and always invalidate relevant
///     cache entries on success.
/// </summary>
/// <param name="logger">Structured logger for diagnostics.</param>
/// <param name="unit">Unit-of-work providing repository access and transaction management.</param>
/// <param name="cache">Cache repository for read-path acceleration.</param>
/// <param name="mapper">AutoMapper instance for entity ↔ DTO projection.</param>
public class CategoryService(
    ILogger<CategoryService> logger,
    IUnitOfWork unit,
    ICacheRepository cache,
    IMapper mapper
) : ICategoryService
{
    /// <summary>Cache key for the flat list of all system (default) categories.</summary>
    private const string CacheKeyAll = "categories:all";

    // ── Front-Office ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<Result<IEnumerable<CategoryResponseDto>>> GetAllAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("Fetching all categories for user {UserId}.", userId);

        var categories = await cache.GetOrCreateAsync<List<CategoryResponseDto>>(
            CacheKeyUserAll(userId),
            async token =>
            {
                var entities = await unit.Categories.GetAllForUserAsync(userId, token);
                return mapper.Map<List<CategoryResponseDto>>(entities);
            },
            ct);

        return Result<IEnumerable<CategoryResponseDto>>.Success(categories ?? []);
    }

    /// <inheritdoc />
    public async Task<Result<CategoryResponseDto>> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("Fetching category {CategoryId} for user {UserId}.", id, userId);

        // Cache the projected DTO directly; CategoryResponseDto preserves both
        // UserId and IsDefault, which are needed for the access check below.
        var dto = await cache.GetOrCreateAsync<CategoryResponseDto>(
            CacheKey(id),
            async token =>
            {
                var entity = await unit.Categories.GetByIdAsync(id, token);
                return entity is null ? null : mapper.Map<CategoryResponseDto>(entity);
            },
            ct);

        if (dto is null)
            return Error.Category.NotFound(id);

        // A category is accessible if it is a system default or belongs to the requesting user.
        if (!dto.IsDefault && dto.UserId != userId)
        {
            logger.LogWarning(
                "User {UserId} attempted to access category {CategoryId} they do not own.", userId, id);
            return Error.General.Forbidden;
        }

        return Result<CategoryResponseDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CategoryResponseDto>> CreateCustomAsync(
        CategoryCreateDto dto,
        Guid userId,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("Creating custom category '{Name}' for user {UserId}.", dto.Name, userId);

        // Serializable isolation prevents two concurrent requests from racing to insert
        // a duplicate category name for the same user.
        await using var tx = await unit.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var nameExists = await unit.Categories.ExistsByNameAsync(dto.Name, userId, ct);
            if (nameExists)
                return Error.Category.NameAlreadyInUse;

            var category = mapper.Map<Category>(dto);
            category.Id = Guid.NewGuid();
            category.UserId = userId;
            category.IsDefault = false;
            category.CreatedAt = DateTime.UtcNow;
            category.CreatedBy = userId.ToString();

            await unit.Categories.CreateAsync(category, ct);
            await unit.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // Invalidate the user's category list so the next read re-fetches from the DB.
            await cache.RemoveAsync(CacheKeyUserAll(userId), ct);

            logger.LogInformation(
                "Custom category {CategoryId} created for user {UserId}.", category.Id, userId);
            return Result<CategoryResponseDto>.Success(mapper.Map<CategoryResponseDto>(category));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating custom category for user {UserId}.", userId);
            await tx.RollbackAsync(ct);
            return Error.General.InternalServerError;
        }
    }

    /// <inheritdoc />
    public async Task<Result<CategoryResponseDto>> UpdateCustomAsync(
        Guid id,
        CategoryUpdateDto dto,
        Guid userId,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("Updating custom category {CategoryId} for user {UserId}.", id, userId);

        // RepeatableRead ensures the category row cannot be modified by another transaction
        // between our read and the subsequent update within this scope.
        await using var tx = await unit.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        try
        {
            var category = await unit.Categories.GetByIdAsync(id, ct);
            if (category is null)
                return Error.Category.NotFound(id);

            // Guard: only the owning user may mutate a custom (non-default) category.
            if (category.IsDefault || category.UserId != userId)
            {
                logger.LogWarning(
                    "User {UserId} attempted to update category {CategoryId} they do not own.", userId, id);
                return Error.General.Forbidden;
            }

            // Name uniqueness check: only performed when the name actually changes.
            if (!string.Equals(category.Name, dto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var nameExists = await unit.Categories.ExistsByNameAsync(dto.Name, userId, ct);
                if (nameExists)
                    return Error.Category.NameAlreadyInUse;
            }

            // Apply DTO fields (Name, Icon, Color) onto the detached entity.
            mapper.Map(dto, category);
            category.ModifiedAt = DateTime.UtcNow;
            category.ModifiedBy = userId.ToString();

            await unit.Categories.UpdateAsync(category, ct);
            await unit.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // Evict both the individual entry and the user's list from cache.
            await cache.RemoveAsync(CacheKey(id), ct);
            await cache.RemoveAsync(CacheKeyUserAll(userId), ct);

            logger.LogInformation(
                "Custom category {CategoryId} updated by user {UserId}.", id, userId);
            return Result<CategoryResponseDto>.Success(mapper.Map<CategoryResponseDto>(category));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unexpected error updating custom category {CategoryId} for user {UserId}.", id, userId);
            await tx.RollbackAsync(ct);
            return Error.General.InternalServerError;
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteCustomAsync(
        Guid id,
        Guid userId,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("Deleting custom category {CategoryId} for user {UserId}.", id, userId);

        // Serializable prevents a concurrent expense from being linked to this category
        // between our linked-expense check and the actual delete.
        await using var tx = await unit.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var category = await unit.Categories.GetByIdAsync(id, ct);
            if (category is null)
                return Error.Category.NotFound(id);

            // Default (system) categories can never be deleted through the front-office.
            if (category.IsDefault)
                return Error.Category.CannotDeleteDefault;

            if (category.UserId != userId)
            {
                logger.LogWarning(
                    "User {UserId} attempted to delete category {CategoryId} they do not own.", userId, id);
                return Error.General.Forbidden;
            }

            var hasExpenses = await unit.Categories.HasLinkedExpensesAsync(id, ct);
            if (hasExpenses)
                return Error.Category.CannotDeleteWithLinkedExpenses;

            await unit.Categories.DeleteAsync(category, ct);
            await unit.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await cache.RemoveAsync(CacheKey(id), ct);
            await cache.RemoveAsync(CacheKeyUserAll(userId), ct);

            logger.LogInformation(
                "Custom category {CategoryId} deleted by user {UserId}.", id, userId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unexpected error deleting custom category {CategoryId} for user {UserId}.", id, userId);
            await tx.RollbackAsync(ct);
            return Error.General.InternalServerError;
        }
    }

    // ── Back-Office ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<Result<IEnumerable<CategoryResponseDto>>> GetAllSystemAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Fetching all system categories.");

        var categories = await cache.GetOrCreateAsync<List<CategoryResponseDto>>(
            CacheKeyAll,
            async token =>
            {
                var entities = await unit.Categories.GetAllSystemAsync(token);
                return mapper.Map<List<CategoryResponseDto>>(entities);
            },
            ct);

        return Result<IEnumerable<CategoryResponseDto>>.Success(categories ?? []);
    }

    /// <inheritdoc />
    public async Task<Result<CategoryResponseDto>> CreateSystemAsync(
        CategoryCreateDto dto,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("Creating system category '{Name}'.", dto.Name);

        // Serializable prevents two concurrent admin requests from creating duplicate
        // system category names.
        await using var tx = await unit.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            // null userId scopes the uniqueness check to system categories only.
            var nameExists = await unit.Categories.ExistsByNameAsync(dto.Name, null, ct);
            if (nameExists)
                return Error.Category.NameAlreadyInUse;

            var category = mapper.Map<Category>(dto);
            category.Id = Guid.NewGuid();
            category.UserId = null;
            category.IsDefault = true;
            category.CreatedAt = DateTime.UtcNow;
            category.CreatedBy = "system";

            await unit.Categories.CreateAsync(category, ct);
            await unit.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // Evict the system list so the next back-office read reflects the new entry.
            await cache.RemoveAsync(CacheKeyAll, ct);

            logger.LogInformation("System category {CategoryId} created.", category.Id);
            return Result<CategoryResponseDto>.Success(mapper.Map<CategoryResponseDto>(category));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating system category '{Name}'.", dto.Name);
            await tx.RollbackAsync(ct);
            return Error.General.InternalServerError;
        }
    }

    /// <inheritdoc />
    public async Task<Result<CategoryResponseDto>> UpdateSystemAsync(
        Guid id,
        CategoryUpdateDto dto,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("Updating system category {CategoryId}.", id);

        await using var tx = await unit.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        try
        {
            var category = await unit.Categories.GetByIdAsync(id, ct);

            // Verify the target is a system category, not a custom user-owned one.
            if (category is null || !category.IsDefault)
                return Error.Category.NotFound(id);

            if (!string.Equals(category.Name, dto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var nameExists = await unit.Categories.ExistsByNameAsync(dto.Name, null, ct);
                if (nameExists)
                    return Error.Category.NameAlreadyInUse;
            }

            mapper.Map(dto, category);
            category.ModifiedAt = DateTime.UtcNow;
            category.ModifiedBy = "system";

            await unit.Categories.UpdateAsync(category, ct);
            await unit.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // Evict both the individual entry and the system list from cache.
            await cache.RemoveAsync(CacheKey(id), ct);
            await cache.RemoveAsync(CacheKeyAll, ct);

            logger.LogInformation("System category {CategoryId} updated.", id);
            return Result<CategoryResponseDto>.Success(mapper.Map<CategoryResponseDto>(category));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating system category {CategoryId}.", id);
            await tx.RollbackAsync(ct);
            return Error.General.InternalServerError;
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteSystemAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogInformation("Deleting system category {CategoryId}.", id);

        await using var tx = await unit.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var category = await unit.Categories.GetByIdAsync(id, ct);
            if (category is null || !category.IsDefault)
                return Error.Category.NotFound(id);

            var hasExpenses = await unit.Categories.HasLinkedExpensesAsync(id, ct);
            if (hasExpenses)
                return Error.Category.CannotDeleteWithLinkedExpenses;

            await unit.Categories.DeleteAsync(category, ct);
            await unit.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await cache.RemoveAsync(CacheKey(id), ct);
            await cache.RemoveAsync(CacheKeyAll, ct);

            logger.LogInformation("System category {CategoryId} deleted.", id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error deleting system category {CategoryId}.", id);
            await tx.RollbackAsync(ct);
            return Error.General.InternalServerError;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────

    /// <summary>Returns the per-ID cache key for a single projected <see cref="CategoryResponseDto" />.</summary>
    private static string CacheKey(Guid id)
    {
        return $"category:{id}";
    }

    /// <summary>Returns the per-user cache key for the full projected category list.</summary>
    private static string CacheKeyUserAll(Guid userId)
    {
        return $"categories:user:{userId}";
    }
}