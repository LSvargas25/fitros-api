using System.Net;
using System.Net.Http.Json;
using FitRos.API.Contracts.ClientProfiles;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.ClientProfiles.GetPhysicalMeasures;
using FitRos.Application.Features.ClientProfiles.ProgressReport.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.MyProgress;

// BACKEND_AUDIT §26.3 / §27 — /api/my/{measurements,progress-report}: JWT-resolved,
// Client + Coach only. Auth is the fixed user 2222…, gym 1111…, role via X-Test-Role.
public class MyProgressApiTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid TestGymId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MeUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public MyProgressApiTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Client");
    }

    private async Task EnsureCallerProfileAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();
        var exists = await db.ClientProfiles.IgnoreQueryFilters().AnyAsync(cp => cp.UserId == MeUserId);
        if (!exists)
        {
            db.ClientProfiles.Add(ClientProfile.Create(TestGymId, MeUserId, Guid.NewGuid()));
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Client_can_record_then_read_their_own_measurement()
    {
        await EnsureCallerProfileAsync();

        var post = await _client.PostAsJsonAsync("/api/my/measurements", new AddPhysicalMeasureRequest
        {
            Weight = 79.5m, BodyFatPercentage = 17m, MuscleMass = 35m,
            Waist = 81m, Chest = 101m, Arms = 39m
        });
        post.StatusCode.Should().Be(HttpStatusCode.NoContent, await post.Content.ReadAsStringAsync());

        var rows = await _client.GetFromJsonAsync<List<PhysicalMeasureHistoryDto>>("/api/my/measurements");
        rows!.Should().Contain(r => r.Weight == 79.5m);
    }

    [Fact]
    public async Task Client_can_get_their_own_progress_report()
    {
        await EnsureCallerProfileAsync();

        var res = await _client.GetAsync("/api/my/progress-report");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await res.Content.ReadFromJsonAsync<ProgressReportDto>();
        dto!.Year.Should().Be(DateTime.UtcNow.Year);
    }

    [Fact]
    public async Task A_back_dated_measurement_is_accepted_but_a_future_one_is_400()
    {
        await EnsureCallerProfileAsync();

        var ok = await _client.PostAsJsonAsync("/api/my/measurements", new AddPhysicalMeasureRequest
        {
            Weight = 80m, RecordedAt = DateTime.UtcNow.AddDays(-5)
        });
        ok.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var future = await _client.PostAsJsonAsync("/api/my/measurements", new AddPhysicalMeasureRequest
        {
            Weight = 80m, RecordedAt = DateTime.UtcNow.AddDays(3)
        });
        future.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("GET", "/api/my/measurements")]
    [InlineData("GET", "/api/my/progress-report")]
    public async Task Staff_are_forbidden(string method, string path)
    {
        var admin = _factory.CreateClient(); // default role Admin

        var res = await admin.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Coach_can_use_the_endpoints_for_their_own_CoachSelf_profile()
    {
        // A CoachSelf profile for the fixed test user, if not already present.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();
            var hasCoachSelf = await db.ClientProfiles.IgnoreQueryFilters()
                .AnyAsync(cp => cp.UserId == MeUserId && cp.CoachId == null);
            if (!hasCoachSelf)
            {
                db.ClientProfiles.Add(ClientProfile.CreateForCoachSelf(TestGymId, MeUserId));
                await db.SaveChangesAsync(CancellationToken.None);
            }
        }

        var coach = _factory.CreateClient();
        coach.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Coach");

        var res = await coach.GetAsync("/api/my/measurements");

        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
