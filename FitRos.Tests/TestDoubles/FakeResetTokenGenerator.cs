using FitRos.Application.Abstractions.Security;

namespace FitRos.Tests.TestDoubles;

public sealed class FakeResetTokenGenerator : IPasswordResetTokenGenerator
{
    public string Generate()
        => Guid.NewGuid().ToString();

    public string Hash(string token)
        => $"hash:{token}";
}