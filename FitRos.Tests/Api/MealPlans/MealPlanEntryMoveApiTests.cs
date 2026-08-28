using System.Net;
using System.Net.Http.Json;
using FitRos.API.Contracts.MealPlans;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.MealPlans.GetMealPlanById;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Nutrition;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.MealPlans;

public class MealPlanEntryMoveApiTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid TestGymId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherGymId = Guid.Parse("44445555-6666-7777-8888-999900001111");

    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public MealPlanEntryMoveApiTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // Plants a plan with 3 foods in Monday/Lunch (order 1,2,3). Returns the plan
    // id and the entry ids in current order.
    private async Task<(Guid planId, List<Guid> entryIds)> PlantPlanWithThreeEntriesAsync(Guid gymId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();

        var profile = ClientProfile.Create(gymId, Guid.NewGuid(), coachId: Guid.NewGuid());
        db.ClientProfiles.Add(profile);

        var plan = MealPlan.Create(profile.Id, Guid.NewGuid(), gymId, $"Plan {Guid.NewGuid()}");

        for (var i = 0; i < 3; i++)
        {
            var food = Food.Create($"Food {Guid.NewGuid()}", FoodCategory.Protein,
                100m, 20m, 0m, 5m, null, gymId);
            db.Foods.Add(food);
            plan.AddEntry(DayOfWeek.Monday, MealType.Lunch, food.Id, 100m);
        }

        db.MealPlans.Add(plan);
        await db.SaveChangesAsync(CancellationToken.None);

        var ids = plan.Entries.OrderBy(e => e.Order).Select(e => e.Id).ToList();
        return (plan.Id, ids);
    }

    private async Task<List<MealPlanEntryDto>> GetEntriesAsync(Guid planId)
    {
        var dto = await _client.GetFromJsonAsync<MealPlanDto>($"/api/meal-plans/{planId}");
        return dto!.Entries;
    }

    [Fact]
    public async Task Moving_an_entry_to_the_front_reorders_the_slot()
    {
        var (planId, ids) = await PlantPlanWithThreeEntriesAsync(TestGymId);

        var move = await _client.PutAsJsonAsync(
            $"/api/meal-plans/{planId}/entries/{ids[2]}/move",
            new MoveMealPlanEntryRequest(1));
        move.StatusCode.Should().Be(HttpStatusCode.NoContent, await move.Content.ReadAsStringAsync());

        var entries = await GetEntriesAsync(planId);
        entries.Single(e => e.Id == ids[2]).Order.Should().Be(1);
        entries.Single(e => e.Id == ids[0]).Order.Should().Be(2);
        entries.Single(e => e.Id == ids[1]).Order.Should().Be(3);
        entries.Select(e => e.Id).Should().ContainInOrder(ids[2], ids[0], ids[1]);
    }

    [Fact]
    public async Task Moving_to_a_position_outside_the_slot_is_400()
    {
        var (planId, ids) = await PlantPlanWithThreeEntriesAsync(TestGymId);

        var move = await _client.PutAsJsonAsync(
            $"/api/meal-plans/{planId}/entries/{ids[0]}/move",
            new MoveMealPlanEntryRequest(99));

        move.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Moving_with_a_non_positive_order_is_400()
    {
        var (planId, ids) = await PlantPlanWithThreeEntriesAsync(TestGymId);

        var move = await _client.PutAsJsonAsync(
            $"/api/meal-plans/{planId}/entries/{ids[0]}/move",
            new MoveMealPlanEntryRequest(0));

        move.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Moving_an_unknown_entry_is_404()
    {
        var (planId, _) = await PlantPlanWithThreeEntriesAsync(TestGymId);

        var move = await _client.PutAsJsonAsync(
            $"/api/meal-plans/{planId}/entries/{Guid.NewGuid()}/move",
            new MoveMealPlanEntryRequest(1));

        move.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unauthenticated_move_is_401()
    {
        var (planId, ids) = await PlantPlanWithThreeEntriesAsync(TestGymId);

        var request = new HttpRequestMessage(
            HttpMethod.Put, $"/api/meal-plans/{planId}/entries/{ids[0]}/move")
        {
            Content = JsonContent.Create(new MoveMealPlanEntryRequest(2))
        };
        request.Headers.Add(TestAuthHandler.UnauthenticatedHeader, "1");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Moving_an_entry_in_a_plan_from_another_gym_is_denied()
    {
        var (planId, ids) = await PlantPlanWithThreeEntriesAsync(OtherGymId);

        var move = await _client.PutAsJsonAsync(
            $"/api/meal-plans/{planId}/entries/{ids[0]}/move",
            new MoveMealPlanEntryRequest(1));

        move.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }
}
