using FitRos.Domain.Entities.Users;

namespace FitRos.Application.Abstractions.Security;

public interface ITokenService
{
    string CreateAccessToken(User user);
    string CreateRefreshTokenPlain();
    string HashToken(string tokenPlain);
}