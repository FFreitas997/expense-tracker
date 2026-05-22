using Domain.Entities;

namespace Infrastructure.Repositories.Interfaces.Resources;

public interface IRecurringExpenseRepository : IRepository<RecurringExpense, Guid>
{
}