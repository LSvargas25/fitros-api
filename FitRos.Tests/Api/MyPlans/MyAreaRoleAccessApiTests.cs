using System.Net;
using System.Net.Http.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Entities.Client;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.MyPlans;

// "My Area" endpoints are Client + Coach only. Admin / OwnerApp -> 403.
// TestApiFactory authenticates every request as UserId 2222…, gym 1111…,
// with the role taken from the X-Test-Role header.
public class MyAreaRoleAccessApiTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid TestGymId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MeUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly TestApiFactory _factory;

    public MyAreaRoleAccessApiTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    private async Task EnsureCallerHasAProfileAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();
        var exists = await db.ClientProfiles.IgnoreQueryFilters()
            .AnyAsync(cp => cp.UserId == MeUserId);
        if (!exists)
        {
            db.ClientProfiles.Add(ClientProfile.Create(TestGymId, MeUserId));
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }

    private HttpClient ClientAs(string role)
    {
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        return c;
    }

    public static IEnumerable<object[]> MyAreaEndpoints => new[]
    {
        new object[] { HttpMethod.Get, "/api/my/meal-plans" },
        new object[] { HttpMethod.Get, "/api/my/training-plans" },
        new object[] { HttpMethod.Get, "/api/client-profiles/me" },
        new object[] { HttpMethod.Get, "/api/workout-sessions/history" },
    };

    [Theory]
    [MemberData(nameof(MyAreaEndpoints))]
    public async Task Coach_is_allowed(HttpMethod method, string path)
    {
        await EnsureCallerHasAProfileAsync();

        var res = await ClientAs("Coach").SendAsync(new HttpRequestMessage(method, path));

        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, $"{method} {path} must be open to Coach");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(MyAreaEndpoints))]
    public async Task Admin_is_forbidden(HttpMethod method, string path)
    {
        var res = await ClientAs("Admin").SendAsync(new HttpRequestMessage(method, path));

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [MemberData(nameof(MyAreaEndpoints))]
    public async Task OwnerApp_is_forbidden(HttpMethod method, string path)
    {
        var res = await ClientAs("OwnerApp").SendAsync(new HttpRequestMessage(method, path));

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Coach_can_start_todays_session_once_they_have_a_profile()
    {
        await EnsureCallerHasAProfileAsync();

        // No active plan / routine seeded, so the handler answers "pick a routine"
        // (404) — the point is it got past the role gate and resolved a profile,
        // not a 403 and not "you have no client profile".
        var res = await ClientAs("Coach").PostAsJsonAsync(
            "/api/workout-sessions/start-today", new { routineId = (Guid?)null });

        res.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        if (res.StatusCode == HttpStatusCode.NotFound)
        {
            var body = await res.Content.ReadAsStringAsync();
            body.Should().NotContain("do not have a client profile");
        }
    }

    [Fact]
    public async Task Admin_cannot_start_todays_session()
    {
        var res = await ClientAs("Admin").PostAsJsonAsync(
            "/api/workout-sessions/start-today", new { routineId = (Guid?)null });

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // The …/active routes 404 (not 200) when there's no active plan, so they can't
    // ride the shared MemberData theories that assert OK for Coach.
    public static IEnumerable<object[]> ActivePlanEndpoints => new[]
    {
        new object[] { "/api/my/meal-plans/active" },
        new object[] { "/api/my/training-plans/active" },
    };

    [Theory]
    [MemberData(nameof(ActivePlanEndpoints))]
    public async Task Active_plan_endpoint_is_open_to_Coach(string path)
    {
        await EnsureCallerHasAProfileAsync();

        var res = await ClientAs("Coach").GetAsync(path);

        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        res.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Theory]
    [MemberData(nameof(ActivePlanEndpoints))]
    public async Task Active_plan_endpoint_is_forbidden_for_Admin(string path)
    {
        var res = await ClientAs("Admin").GetAsync(path);

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [MemberData(nameof(ActivePlanEndpoints))]
    public async Task Active_plan_endpoint_is_forbidden_for_OwnerApp(string path)
    {
        var res = await ClientAs("OwnerApp").GetAsync(path);

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
