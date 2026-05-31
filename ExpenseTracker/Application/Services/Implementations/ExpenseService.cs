using Application.Services.Interfaces;
using Infrastructure.Cache.Interfaces;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

public class ExpenseService(
    ILogger<ExpenseService> logger,
    IUnitOfWork unit,
    ICacheRepository cache
) : IExpenseService
{
}