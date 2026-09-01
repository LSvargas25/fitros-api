using System.Security.Cryptography;
using System.Text;
using FitRos.Application.Abstractions.Security;

namespace FitRos.Infrastructure.Security;

public sealed class EmailVerificationCodeGenerator
    : IEmailVerificationCodeGenerator
{
    public string GenerateCode()
        => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public string Hash(string code)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(bytes);
    }
}
