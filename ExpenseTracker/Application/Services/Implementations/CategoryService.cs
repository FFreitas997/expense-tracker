using Application.Services.Interfaces;
using Infrastructure.Repositories.Interfaces;
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

    private static string CacheKey(Guid id)
    {
        return $"category:{id}";
    }
}