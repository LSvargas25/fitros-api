using System.Net;
using System.Net.Http.Json;
using FitRos.API.Contracts.MealPlans;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.MealPlans.CreateMealPlan;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Nutrition;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.MealPlans;

public class MealPlansApiTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid TestGymId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherGymId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public MealPlansApiTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> PlantClientProfileAsync(Guid gymId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();
        var profile = ClientProfile.Create(gymId, Guid.NewGuid());
        db.ClientProfiles.Add(profile);
        await db.SaveChangesAsync(CancellationToken.None);
        return profile.Id;
    }

    private async Task<Guid> PlantForeignMealPlanAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();
        var profile = ClientProfile.Create(OtherGymId, Guid.NewGuid());
        db.ClientProfiles.Add(profile);
        var plan = MealPlan.Create(profile.Id, Guid.NewGuid(), OtherGymId, $"Foreign {Guid.NewGuid()}");
        db.MealPlans.Add(plan);
        await db.SaveChangesAsync(CancellationToken.None);
        return plan.Id;
    }

    // ---------- happy path ----------

    [Fact]
    public async Task Create_rename_activate_archive_a_plan_for_an_own_gym_client()
    {
        var profileId = await PlantClientProfileAsync(TestGymId);

        var create = await _client.PostAsJsonAsync("/api/meal-plans",
            new CreateMealPlanCommand(profileId, "Cut phase"));
        create.StatusCode.Should().Be(HttpStatusCode.Created, await create.Content.ReadAsStringAsync());
        var created = await create.Content.ReadFromJsonAsync<CreateMealPlanResponse>();

        var get = await _client.GetAsync($"/api/meal-plans/{created!.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var rename = await _client.PatchAsJsonAsync(
            $"/api/meal-plans/{created.Id}/rename", new RenameMealPlanRequest("Cut phase v2"));
        rename.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var activate = await _client.PatchAsync($"/api/meal-plans/{created.Id}/activate", null);
        activate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var archive = await _client.PatchAsync($"/api/meal-plans/{created.Id}/archive", null);
        archive.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Create_for_a_client_in_another_gym_is_denied()
    {
        var foreignProfileId = await PlantClientProfileAsync(OtherGymId);

        var response = await _client.PostAsJsonAsync("/api/meal-plans",
            new CreateMealPlanCommand(foreignProfileId, "nope"));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    // ---------- auth ----------

    [Fact]
    public async Task Unauthenticated_create_is_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/meal-plans")
        {
            Content = JsonContent.Create(new CreateMealPlanCommand(Guid.NewGuid(), "x"))
        };
        request.Headers.Add(TestAuthHandler.UnauthenticatedHeader, "1");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- tenant isolation ----------

    [Fact]
    public async Task Get_by_id_of_a_foreign_gyms_plan_is_denied()
    {
        var id = await PlantForeignMealPlanAsync();

        var response = await _client.GetAsync($"/api/meal-plans/{id}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rename_of_a_foreign_gyms_plan_is_denied()
    {
        var id = await PlantForeignMealPlanAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/meal-plans/{id}/rename", new RenameMealPlanRequest("hijacked"));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Archive_of_a_foreign_gyms_plan_is_denied()
    {
        var id = await PlantForeignMealPlanAsync();

        var response = await _client.PatchAsync($"/api/meal-plans/{id}/archive", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }
}
