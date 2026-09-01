namespace FitRos.Application.Abstractions.Security;

public interface IEmailVerificationCodeGenerator
{
    string GenerateCode();
    string Hash(string code);
}
