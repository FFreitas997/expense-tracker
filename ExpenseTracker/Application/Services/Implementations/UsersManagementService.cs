using Application.Services.Interfaces;
using Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

public class UsersManagementService(
    ILogger<UsersManagementService> logger,
    IUnitOfWork unit,
    ICacheRepository cache
) : IUsersManagementService
{
}