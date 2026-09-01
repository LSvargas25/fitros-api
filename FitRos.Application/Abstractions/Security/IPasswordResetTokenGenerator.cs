namespace FitRos.Application.Abstractions.Security;

public interface IPasswordResetTokenGenerator
{
    string Generate();
    string Hash(string token);
}