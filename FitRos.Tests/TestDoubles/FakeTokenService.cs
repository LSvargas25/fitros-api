using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Entities.Users;

namespace FitRos.Tests.TestDoubles;

public class FakeTokenService : ITokenService
{
    public string CreateAccessToken(User user)
        => "fake-access-token";

    public string CreateRefreshTokenPlain()
        => "fake-refresh-token";

    public string HashToken(string tokenPlain)
        => "hashed-" + tokenPlain;
}