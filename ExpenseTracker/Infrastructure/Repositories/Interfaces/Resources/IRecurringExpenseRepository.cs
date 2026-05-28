using Domain.Entities;
using Infrastructure.Repositories.Queries.RecurringExpense;

namespace Infrastructure.Repositories.Interfaces.Resources;

public interface IRecurringExpenseRepository : IRepository<RecurringExpense, Guid>,
    IPageable<RecurringExpense, RecurringExpenseQuery>;