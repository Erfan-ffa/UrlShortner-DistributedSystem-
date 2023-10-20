using MiniUrl.Entities;

namespace MiniUrl.DataAccess.Contracts;

public interface IUserRepository
{
    Task<User> CreateUserAsync(User user, CancellationToken cancellationToken);
    
    Task<bool> DoesUserExistByUserNameAsync(string username, CancellationToken cancellationToken);
    
    Task<User> GetUserByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<long> GetUserAllowedUrlCreationCountAsync(Guid userId);
}