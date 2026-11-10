using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace FitRos.Tests.Api.Security;

/// <summary>
/// The JWT signing key is validated with the other required settings
/// (RequiredSettingsExtensions): the API must refuse to boot with a key that
/// is too short for HS256 or is still the checked-in placeholder.
/// </summary>
public class JwtSigningKeyGuardTests
{
    private static WebApplicationFactory<Program> FactoryWithKey(string key, string? environment = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            if (environment is not null)
                builder.UseEnvironment(environment);

            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = key }));
        });

    [Fact]
    public void Startup_throws_when_the_signing_key_is_too_short()
    {
        using var factory = FactoryWithKey("too-short");

        var act = () => factory.CreateClient();

        act.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("shorter than 32 bytes").And.Contain("Jwt__SigningKey");
    }

    [Fact]
    public void Startup_accepts_a_key_of_exactly_32_bytes()
    {
        using var factory = FactoryWithKey(new string('k', 32));

        var act = () => factory.CreateClient();

        act.Should().NotThrow();
    }

    [Fact]
    public void Startup_throws_in_production_when_the_key_is_still_the_placeholder()
    {
        // Long enough to pass the length rule, so only the placeholder rule fires.
        using var factory = FactoryWithKey("REPLACE_VIA_ENV_VAR_Jwt__SigningKey_min-32-chars", "Production");

        var act = () => factory.CreateClient();

        act.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("Jwt__SigningKey").And.NotContain("shorter than");
    }
}
