using FitRos.Application.Abstractions.Security;

namespace FitRos.Tests.TestDoubles;

public class FakeResetTokenGenerator : IPasswordResetTokenGenerator
{
    public string Hash(string token)
    {
        return $"HASH_{token}";
    }

    public string Generate()
    {
        return "fake-token";
    }
}