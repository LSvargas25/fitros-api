using System.Security.Cryptography;
using System.Text;
using FitRos.Application.Abstractions.Security;

namespace FitRos.Infrastructure.Security;

public sealed class PasswordResetTokenGenerator
    : IPasswordResetTokenGenerator
{
    public string Generate()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public string Hash(string token)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}