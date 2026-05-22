using Application.Services.Interfaces;
using Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

public class CategoryService(
    ILogger<CategoryService> logger,
    IUnitOfWork unit,
    ICacheRepository cache
) : ICategoryService
{
    private const string CacheKeyAll = "categories:all";

    private static string CacheKey(Guid id)
    {
        return $"category:{id}";
    }
}