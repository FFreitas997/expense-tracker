using Application.Services.Interfaces;
using Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

public class BudgetService(
    ILogger<BudgetService> logger,
    IUnitOfWork unit,
    ICacheRepository cache
) : IBudgetService
{
}