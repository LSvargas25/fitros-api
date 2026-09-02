using System.Net;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Api.Health;

public class HealthApiTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public HealthApiTests(TestApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_returns_200_ok_for_an_unauthenticated_request()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UnauthenticatedHeader, "1");

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"status\"").And.Contain("\"ok\"");
    }
}
