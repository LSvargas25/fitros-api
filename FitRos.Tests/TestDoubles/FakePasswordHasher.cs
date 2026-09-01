using FitRos.Application.Abstractions.Security;

namespace FitRos.Tests.TestDoubles;

public class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password)
        => $"HASHED_{password}";

    // IPasswordHasher.Verify is declared (hash, password) but every real
    // caller passes (plaintext, storedHash) positionally - match that here
    // too, since PasswordHasherAdapter's implementation does the same.
    public bool Verify(string password, string hash)
        => hash == $"HASHED_{password}";
}