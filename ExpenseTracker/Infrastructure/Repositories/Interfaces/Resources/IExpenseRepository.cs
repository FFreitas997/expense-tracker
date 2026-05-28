using Domain.Entities;
using Infrastructure.Repositories.Queries.Expense;

namespace Infrastructure.Repositories.Interfaces.Resources;

public interface IExpenseRepository : IRepository<Expense, Guid>, IPageable<Expense, ExpenseQuery>;