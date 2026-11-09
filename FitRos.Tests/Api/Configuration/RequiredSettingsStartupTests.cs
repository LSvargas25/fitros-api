using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace FitRos.Tests.Api.Configuration;

/// <summary>
/// The API must refuse to start, naming the env var to set, while a secret is
/// empty or still the REPLACE_VIA_ENV_VAR_* placeholder from appsettings.json.
/// </summary>
public class RequiredSettingsStartupTests
{
    [Theory]
    [InlineData("ConnectionStrings:DefaultConnection",
        "Host=localhost;Database=fitros;Username=postgres;Password=REPLACE_VIA_ENV_VAR_ConnectionStrings__DefaultConnection",
        "ConnectionStrings__DefaultConnection")]
    [InlineData("Jwt:SigningKey", "REPLACE_VIA_ENV_VAR_Jwt__SigningKey", "Jwt__SigningKey")]
    [InlineData("Jwt:SigningKey", "", "Jwt__SigningKey")]
    [InlineData("Smtp:Password", "REPLACE_VIA_ENV_VAR_Smtp__Password", "Smtp__Password")]
    [InlineData("Smtp:From", "REPLACE_VIA_ENV_VAR_Smtp__From", "Smtp__From")]
    [InlineData("Authentication:Google:ClientId",
        "REPLACE_VIA_ENV_VAR_Authentication__Google__ClientId", "Authentication__Google__ClientId")]
    public void Startup_WithPlaceholderOrEmptySetting_FailsNamingTheEnvVar(
        string key, string value, string expectedEnvVar)
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })));

        var act = () => factory.CreateClient();

        act.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain(expectedEnvVar);
    }

    [Fact]
    public void Startup_WithAllSettingsConfigured_Starts()
    {
        using var factory = new WebApplicationFactory<Program>();

        var act = () => factory.CreateClient();

        act.Should().NotThrow();
    }
}
