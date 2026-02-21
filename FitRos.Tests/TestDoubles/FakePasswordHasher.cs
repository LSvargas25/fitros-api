using FitRos.Application.Abstractions.Security;

namespace FitRos.Tests.TestDoubles;

public class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password)
        => $"HASHED_{password}";

    public bool Verify(string hash, string password)
        => hash == $"HASHED_{password}";
}