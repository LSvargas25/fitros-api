using FitRos.Application.Abstractions.Security;

namespace FitRos.Tests.TestDoubles;

public sealed class FakeResetTokenGenerator
    : IPasswordResetTokenGenerator
{
    public string Generate() => "fake-token";

    public string Hash(string token) => "hashed-fake-token";
}