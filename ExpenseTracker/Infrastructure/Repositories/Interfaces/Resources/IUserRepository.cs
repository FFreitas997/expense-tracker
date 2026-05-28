using Domain.Entities;
using Infrastructure.Repositories.Queries.User;

namespace Infrastructure.Repositories.Interfaces.Resources;

public interface IUserRepository : IRepository<User, Guid>, IPageable<User, UserQuery>;