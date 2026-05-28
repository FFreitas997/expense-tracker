using Domain.Entities;
using Infrastructure.Repositories.Queries.Budget;

namespace Infrastructure.Repositories.Interfaces.Resources;

public interface IBudgetRepository : IRepository<Budget, Guid>, IPageable<Budget, BudgetQuery>;