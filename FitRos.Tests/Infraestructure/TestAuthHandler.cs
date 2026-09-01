using System.Security.Claims;
using System.Text.Encodings.Web;
using FitRos.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FitRos.Tests.Infrastructure;

/// <summary>
/// Test authentication scheme for API tests. Every request is authenticated as
/// the same Admin principal that <see cref="TestApiFactory"/>'s fake
/// <c>ICurrentUser</c> represents, so <c>[Authorize]</c>/<c>[Authorize(Roles=…)]</c>
/// controllers behave. A request that sends the <c>X-Test-Unauthenticated</c>
/// header is left unauthenticated, so tests can assert 401.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string UnauthenticatedHeader = "X-Test-Unauthenticated";

    public static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid GymId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static UserRole Role { get; set; } = UserRole.Admin;

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey(UnauthenticatedHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
            new Claim(ClaimTypes.Role, Role.ToString()),
            new Claim("gymId", GymId.ToString()),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
