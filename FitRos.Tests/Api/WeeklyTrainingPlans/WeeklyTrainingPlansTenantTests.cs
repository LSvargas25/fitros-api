using System.Net;
using System.Net.Http.Json;
using FitRos.API.Contracts.WeeklyTrainingPlans;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.WeeklyTrainingPlans.CreatePlan;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.WeeklyTraining;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.WeeklyTrainingPlans;

// The existing WeeklyTrainingPlansApiTests only checks 401 (raw factory). This
// suite runs authenticated (Admin, gym 11111111-…) and exercises the happy
// path plus cross-gym isolation.
public class WeeklyTrainingPlansTenantTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid TestGymId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherGymId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public WeeklyTrainingPlansTenantTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> PlantClientProfileAsync(Guid gymId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();
        // A coach must be assigned — the create handler stamps the plan's CoachId
        // from the profile and the domain rejects Guid.Empty.
        var profile = ClientProfile.Create(gymId, Guid.NewGuid(), coachId: Guid.NewGuid());
        db.ClientProfiles.Add(profile);
        await db.SaveChangesAsync(CancellationToken.None);
        return profile.Id;
    }

    private async Task<Guid> PlantForeignPlanAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();
        var profile = ClientProfile.Create(OtherGymId, Guid.NewGuid());
        db.ClientProfiles.Add(profile);
        var plan = WeeklyTrainingPlan.Create(profile.Id, Guid.NewGuid(), OtherGymId, $"Foreign {Guid.NewGuid()}");
        db.WeeklyTrainingPlans.Add(plan);
        await db.SaveChangesAsync(CancellationToken.None);
        return plan.Id;
    }

    [Fact]
    public async Task Create_then_rename_then_archive_for_an_own_gym_client()
    {
        var profileId = await PlantClientProfileAsync(TestGymId);

        var create = await _client.PostAsJsonAsync("/api/training-plans",
            new CreateWeeklyTrainingPlanCommand(profileId, "Week A"));
        create.StatusCode.Should().Be(HttpStatusCode.Created, await create.Content.ReadAsStringAsync());
        var created = await create.Content.ReadFromJsonAsync<CreateWeeklyTrainingPlanResponse>();

        var get = await _client.GetAsync($"/api/training-plans/{created!.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var rename = await _client.PatchAsJsonAsync(
            $"/api/training-plans/{created.Id}/rename", new RenamePlanRequest("Week A v2"));
        rename.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var archive = await _client.PatchAsync($"/api/training-plans/{created.Id}/archive", null);
        archive.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Create_for_a_client_in_another_gym_is_forbidden()
    {
        var foreignProfileId = await PlantClientProfileAsync(OtherGymId);

        var response = await _client.PostAsJsonAsync("/api/training-plans",
            new CreateWeeklyTrainingPlanCommand(foreignProfileId, "nope"));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_by_id_of_a_foreign_gyms_plan_is_denied()
    {
        var id = await PlantForeignPlanAsync();

        var response = await _client.GetAsync($"/api/training-plans/{id}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Rename_of_a_foreign_gyms_plan_is_denied()
    {
        var id = await PlantForeignPlanAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/training-plans/{id}/rename", new RenamePlanRequest("hijacked"));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Archive_of_a_foreign_gyms_plan_is_denied()
    {
        var id = await PlantForeignPlanAsync();

        var response = await _client.PatchAsync($"/api/training-plans/{id}/archive", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }
}
