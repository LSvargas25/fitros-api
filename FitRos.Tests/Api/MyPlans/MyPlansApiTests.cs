using System.Net;
using System.Net.Http.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.MealPlans.GetClientMealPlans;
using FitRos.Application.Features.MealPlans.GetMealPlanById;
using FitRos.Application.Features.WeeklyTrainingPlans.GetClientPlans;
using FitRos.Application.Features.WeeklyTrainingPlans.GetPlanById;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Nutrition;
using FitRos.Domain.Entities.WeeklyTraining;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.MyPlans;

// Authenticated as a Client (X-Test-Role) whose user id is TestAuthHandler.UserId,
// in gym 11111111-…. Proves a Client sees only their own plans.
public class MyPlansApiTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid TestGymId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MeUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public MyPlansApiTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Client");
    }

    private sealed record Seed(Guid MyMealPlanId, Guid MyTrainingPlanId,
                               Guid OtherMealPlanId, Guid OtherTrainingPlanId);

    private async Task<Seed> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();

        // The shared store persists between tests, so reuse the caller's own
        // ClientProfile if one is already there (CallerClientProfile resolves
        // the first match by UserId).
        var mine = db.ClientProfiles.IgnoreQueryFilters()
            .FirstOrDefault(cp => cp.UserId == MeUserId)
            ?? ClientProfile.Create(TestGymId, MeUserId, Guid.NewGuid());

        var other = ClientProfile.Create(TestGymId, Guid.NewGuid(), Guid.NewGuid());

        if (db.Entry(mine).State == EntityState.Detached)
            db.ClientProfiles.Add(mine);
        db.ClientProfiles.Add(other);

        var myMeal = MealPlan.Create(mine.Id, Guid.NewGuid(), TestGymId, "My cut");
        var myTraining = WeeklyTrainingPlan.Create(mine.Id, Guid.NewGuid(), TestGymId, "My week");
        var otherMeal = MealPlan.Create(other.Id, Guid.NewGuid(), TestGymId, "Someone else");
        var otherTraining = WeeklyTrainingPlan.Create(other.Id, Guid.NewGuid(), TestGymId, "Someone else");
        db.MealPlans.AddRange(myMeal, otherMeal);
        db.WeeklyTrainingPlans.AddRange(myTraining, otherTraining);

        await db.SaveChangesAsync(CancellationToken.None);
        return new Seed(myMeal.Id, myTraining.Id, otherMeal.Id, otherTraining.Id);
    }

    // ---------- my data → 200 ----------

    [Fact]
    public async Task Client_sees_only_their_own_meal_plans_in_the_list()
    {
        var seed = await SeedAsync();

        var rows = await _client.GetFromJsonAsync<List<MealPlanListItemDto>>("/api/my/meal-plans");

        rows!.Select(r => r.Id).Should().Contain(seed.MyMealPlanId)
            .And.NotContain(seed.OtherMealPlanId);
    }

    [Fact]
    public async Task Client_can_read_their_own_meal_plan_detail()
    {
        var seed = await SeedAsync();

        var response = await _client.GetAsync($"/api/my/meal-plans/{seed.MyMealPlanId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<MealPlanDto>())!.Id.Should().Be(seed.MyMealPlanId);
    }

    [Fact]
    public async Task Client_sees_only_their_own_training_plans_in_the_list()
    {
        var seed = await SeedAsync();

        var rows = await _client.GetFromJsonAsync<List<TrainingPlanListItemDto>>("/api/my/training-plans");

        rows!.Select(r => r.Id).Should().Contain(seed.MyTrainingPlanId)
            .And.NotContain(seed.OtherTrainingPlanId);
    }

    [Fact]
    public async Task Client_can_read_their_own_training_plan_detail()
    {
        var seed = await SeedAsync();

        var response = await _client.GetAsync($"/api/my/training-plans/{seed.MyTrainingPlanId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<WeeklyTrainingPlanDto>())!.Id.Should().Be(seed.MyTrainingPlanId);
    }

    // ---------- someone else's data → 404 ----------

    [Fact]
    public async Task Reading_another_clients_meal_plan_is_404()
    {
        var seed = await SeedAsync();

        var response = await _client.GetAsync($"/api/my/meal-plans/{seed.OtherMealPlanId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reading_another_clients_training_plan_is_404()
    {
        var seed = await SeedAsync();

        var response = await _client.GetAsync($"/api/my/training-plans/{seed.OtherTrainingPlanId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- writes still blocked ----------

    [Theory]
    [InlineData("POST", "/api/meal-plans")]
    [InlineData("POST", "/api/training-plans")]
    [InlineData("POST", "/api/exercises")]
    [InlineData("POST", "/api/foods")]
    public async Task Client_still_cannot_write(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = JsonContent.Create(new { })
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------- catalog reads open to Client ----------

    [Theory]
    [InlineData("/api/exercises")]
    [InlineData("/api/foods")]
    public async Task Client_can_read_the_catalog(string path)
    {
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------- /api/my is client-only ----------

    [Fact]
    public async Task Staff_cannot_use_the_my_endpoints()
    {
        var staff = _factory.CreateClient(); // default role = Admin

        var response = await staff.GetAsync("/api/my/meal-plans");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
