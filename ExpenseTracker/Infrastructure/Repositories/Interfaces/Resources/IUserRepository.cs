using Domain.Entities;

namespace Infrastructure.Repositories.Interfaces.Resources;

public interface IUserRepository : IRepository<User, Guid>
{
}