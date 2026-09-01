using FitRos.Application.Abstractions.Security;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace FitRos.Infrastructure.Security;

public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleAuthSettings _settings;

    public GoogleTokenValidator(IOptions<GoogleAuthSettings> options)
    {
        _settings = options.Value;
    }

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken ct)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _settings.ClientId }
                });

            return new GoogleUserInfo(
                payload.Email,
                payload.EmailVerified,
                string.IsNullOrWhiteSpace(payload.GivenName) ? "Google" : payload.GivenName,
                string.IsNullOrWhiteSpace(payload.FamilyName) ? "User" : payload.FamilyName);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
