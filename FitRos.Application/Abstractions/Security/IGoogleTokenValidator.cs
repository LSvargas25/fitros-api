namespace FitRos.Application.Abstractions.Security;

public sealed record GoogleUserInfo(
    string Email,
    bool EmailVerified,
    string FirstName,
    string LastName
);

public interface IGoogleTokenValidator
{
    /// <summary>Returns null when the token is missing, expired, or doesn't match our Client ID.</summary>
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken ct);
}
