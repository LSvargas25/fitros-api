using FitRos.Application.Abstractions.Security;

namespace FitRos.Tests.TestDoubles;

public class FakeEmailVerificationCodeGenerator : IEmailVerificationCodeGenerator
{
    public string GenerateCode()
        => "123456";

    public string Hash(string code)
        => $"HASH_{code}";
}
