using FitRos.API;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Behaviors;
using FitRos.Application.Features.Users.Coach.CreateCoach;
using FitRos.Application.Features.Users.UserManagement.CreateUser;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FitRos.Tests.Api.Coaches;

/// <summary>
/// Factory that authenticates as OwnerApp — required for OwnerApp-only endpoints (DeleteCoach).
/// </summary>
public class CoachOwnerAppTestApiFactory : WebApplicationFactory<Program>
{
    private static readonly string DbName = "FitRosCoachOwnerAppTestDb";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICurrentUser>();
            services.AddScoped<ICurrentUser>(_ =>
                new FakeCurrentUser(null)
                {
                    UserId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    GymId = null,
                    Role = UserRole.OwnerApp,
                    IsAuthenticated = true
                });

            services.RemoveAll<DbContextOptions<FitRosDbContext>>();
            services.RemoveAll<IFitRosDbContext>();
            services.AddDbContext<FitRosDbContext>(options =>
                options.UseInMemoryDatabase(DbName));
            services.AddScoped<IFitRosDbContext>(sp =>
                sp.GetRequiredService<FitRosDbContext>());

            services.RemoveAll(typeof(IPipelineBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthenticationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantGuardBehavior<,>));
        });
    }
}

// ─────────────────────────────────────────────────────────────
// 403 tests — default TestApiFactory uses Role=Admin,
// which is NOT allowed on OwnerApp-only endpoints.
// ─────────────────────────────────────────────────────────────

public class CoachesApiTests_Forbidden : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public CoachesApiTests_Forbidden(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DeleteCoach_Returns_403_When_Role_Is_Admin()
    {
        var response = await _client.DeleteAsync($"/api/coaches/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

// ─────────────────────────────────────────────────────────────
// Admin-accessible tests — default TestApiFactory (Admin role)
// ─────────────────────────────────────────────────────────────

public class CoachesApiTests_Admin : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public CoachesApiTests_Admin(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ActivateCoach_Returns_404_When_Coach_Not_Found()
    {
        var response = await _client.PatchAsync(
            $"/api/coaches/{Guid.NewGuid()}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateCoach_Returns_404_When_Coach_Not_Found()
    {
        var response = await _client.PatchAsync(
            $"/api/coaches/{Guid.NewGuid()}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCoaches_Returns_200()
    {
        var response = await _client.GetAsync("/api/coaches");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCoachById_Returns_404_When_Not_Found()
    {
        var response = await _client.GetAsync($"/api/coaches/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

// ─────────────────────────────────────────────────────────────
// Success + 404 tests — OwnerApp factory
// ─────────────────────────────────────────────────────────────

public class CoachesApiTests_OwnerApp : IClassFixture<CoachOwnerAppTestApiFactory>
{
    private readonly HttpClient _client;

    public CoachesApiTests_OwnerApp(CoachOwnerAppTestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── helpers ───────────────────────────────────────────────

    private async Task<CreateUserResponse> CreateCoachAsync(string email)
    {
        var command = new CreateCoachCommand(email, "Test", "Coach", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/coaches", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<CreateUserResponse>())!;
    }

    private async Task DeactivateCoachAsync(Guid id)
    {
        var response = await _client.PatchAsync($"/api/coaches/{id}/deactivate", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
    }

    // ─── 404 tests ─────────────────────────────────────────────

    [Fact]
    public async Task ActivateCoach_Returns_404_When_Coach_Not_Found()
    {
        var response = await _client.PatchAsync(
            $"/api/coaches/{Guid.NewGuid()}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateCoach_Returns_404_When_Coach_Not_Found()
    {
        var response = await _client.PatchAsync(
            $"/api/coaches/{Guid.NewGuid()}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCoach_Returns_404_When_Coach_Not_Found()
    {
        var response = await _client.DeleteAsync(
            $"/api/coaches/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── 204 success tests ─────────────────────────────────────

    [Fact]
    public async Task DeactivateCoach_Returns_204_On_Success()
    {
        var coach = await CreateCoachAsync($"{Guid.NewGuid()}@deactivate.com");

        var response = await _client.PatchAsync(
            $"/api/coaches/{coach.Id}/deactivate", null);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }

    [Fact]
    public async Task ActivateCoach_Returns_204_On_Success()
    {
        var coach = await CreateCoachAsync($"{Guid.NewGuid()}@activate.com");
        await DeactivateCoachAsync(coach.Id);

        var response = await _client.PatchAsync(
            $"/api/coaches/{coach.Id}/activate", null);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }

    [Fact]
    public async Task DeleteCoach_Returns_204_On_Success()
    {
        var coach = await CreateCoachAsync($"{Guid.NewGuid()}@delete.com");
        await DeactivateCoachAsync(coach.Id);

        var response = await _client.DeleteAsync($"/api/coaches/{coach.Id}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }

    [Fact]
    public async Task GetCoaches_Returns_200_With_Created_Coach()
    {
        await CreateCoachAsync($"{Guid.NewGuid()}@list.com");

        var response = await _client.GetAsync("/api/coaches");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
    }

    [Fact]
    public async Task GetCoachById_Returns_200_With_Coach_Data()
    {
        var coach = await CreateCoachAsync($"{Guid.NewGuid()}@getbyid.com");

        var response = await _client.GetAsync($"/api/coaches/{coach.Id}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
    }
}
