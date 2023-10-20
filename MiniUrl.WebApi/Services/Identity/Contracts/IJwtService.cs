using MiniUrl.Entities;

namespace MiniUrl.Services.Identity.Contracts;

public interface IJwtService
{
    string GenerateToken(User user);
}