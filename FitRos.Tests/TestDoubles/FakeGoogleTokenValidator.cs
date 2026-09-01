using FitRos.Application.Abstractions.Security;

namespace FitRos.Tests.TestDoubles;

public class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleUserInfo? _result;

    public FakeGoogleTokenValidator(GoogleUserInfo? result)
    {
        _result = result;
    }

    public Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken ct)
        => Task.FromResult(_result);
}
