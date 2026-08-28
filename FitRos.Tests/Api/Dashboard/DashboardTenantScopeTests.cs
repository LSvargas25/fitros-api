using System.Net;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Api.Dashboard;

// The dashboard returns platform-wide, cross-gym data. TestApiFactory
// authenticates as an Admin, who must NOT be able to see it.
public class DashboardTenantScopeTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public DashboardTenantScopeTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Admin_cannot_read_the_platform_dashboard()
    {
        var response = await _client.GetAsync("/api/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
