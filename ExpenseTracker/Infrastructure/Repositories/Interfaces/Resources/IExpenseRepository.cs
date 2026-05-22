using Domain.Entities;

namespace Infrastructure.Repositories.Interfaces.Resources;

public interface IExpenseRepository : IRepository<Expense, Guid>
{
}