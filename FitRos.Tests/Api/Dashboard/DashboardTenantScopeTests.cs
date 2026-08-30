using System.Net;
using System.Net.Http.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.Dashboard.Queries;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Gym;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.Dashboard;

// GET /api/dashboard: OwnerApp = platform-wide, Admin/Coach = their own gym,
// Client = 403. TestApiFactory authenticates as TestGymId and takes the role
// from the X-Test-Role header.
public class DashboardTenantScopeTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public DashboardTenantScopeTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private HttpRequestMessage Dashboard(string role)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard");
        req.Headers.Add(TestAuthHandler.RoleHeader, role);
        return req;
    }

    private async Task<Guid> PlantForeignGymWithAClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();
        var gym = Gym.Create($"Foreign {Guid.NewGuid()}", "elsewhere", "000");
        db.Gyms.Add(gym);
        db.ClientProfiles.Add(ClientProfile.Create(gym.Id, Guid.NewGuid()));
        await db.SaveChangesAsync(CancellationToken.None);
        return gym.Id;
    }

    [Fact]
    public async Task Client_cannot_read_the_dashboard()
    {
        var response = await _client.SendAsync(Dashboard("Client"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("OwnerApp")]
    [InlineData("Admin")]
    [InlineData("Coach")]
    public async Task Staff_roles_can_read_the_dashboard(string role)
    {
        var response = await _client.SendAsync(Dashboard(role));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_and_Coach_never_see_a_foreign_gym_but_OwnerApp_does()
    {
        var foreignGymId = await PlantForeignGymWithAClientAsync();

        var admin = await (await _client.SendAsync(Dashboard("Admin")))
            .Content.ReadFromJsonAsync<DashboardStatsResponse>();
        var coach = await (await _client.SendAsync(Dashboard("Coach")))
            .Content.ReadFromJsonAsync<DashboardStatsResponse>();
        var owner = await (await _client.SendAsync(Dashboard("OwnerApp")))
            .Content.ReadFromJsonAsync<DashboardStatsResponse>();

        admin!.Gyms.Should().NotContain(g => g.Id == foreignGymId);
        coach!.Gyms.Should().NotContain(g => g.Id == foreignGymId);
        owner!.Gyms.Should().Contain(g => g.Id == foreignGymId);
    }
}
