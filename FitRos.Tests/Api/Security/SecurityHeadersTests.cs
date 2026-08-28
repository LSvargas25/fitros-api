using System.Net.Http.Json;
using FitRos.Tests.Api.Auth;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Api.Security;

public class SecurityHeadersTests : IClassFixture<AuthTestApiFactory>
{
    private readonly HttpClient _client;

    public SecurityHeadersTests(AuthTestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Every_response_carries_the_defensive_headers()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "owner@fitros.com", password = "Owner123!" });

        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle().Which.Should().Be("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle().Which.Should().Be("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle().Which.Should().Be("no-referrer");
        response.Headers.GetValues("Content-Security-Policy").Should().ContainSingle()
            .Which.Should().Contain("frame-ancestors 'none'");
        response.Headers.Contains("Permissions-Policy").Should().BeTrue();
    }

    [Fact]
    public async Task Non_swagger_responses_get_the_locked_down_csp()
    {
        var response = await _client.GetAsync("/api/auth/does-not-exist");

        response.Headers.GetValues("Content-Security-Policy").Should().ContainSingle()
            .Which.Should().Be("default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'");
    }

    [Fact]
    public async Task Server_header_is_not_advertised()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "owner@fitros.com", password = "Owner123!" });

        response.Headers.Contains("Server").Should().BeFalse();
    }
}
