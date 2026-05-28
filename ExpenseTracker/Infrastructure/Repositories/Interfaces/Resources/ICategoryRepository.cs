using Domain.Entities;
using Infrastructure.Repositories.Queries.Category;

namespace Infrastructure.Repositories.Interfaces.Resources;

public interface ICategoryRepository : IRepository<Category, Guid>, IPageable<Category, CategoryQuery>;