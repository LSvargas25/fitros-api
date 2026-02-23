using FitRos.Application.Abstractions.Security;
using Microsoft.AspNetCore.Identity;

namespace FitRos.Infrastructure.Security;

public class PasswordHasherAdapter : IPasswordHasher
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Dummy = new();

    public string Hash(string password) =>
        Hasher.HashPassword(Dummy, password);

    public bool Verify(string hash, string password) =>
        Hasher.VerifyHashedPassword(Dummy, hash, password)
            == PasswordVerificationResult.Success;
}