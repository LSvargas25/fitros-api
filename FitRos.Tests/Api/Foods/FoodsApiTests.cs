using System.Net;
using System.Net.Http.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.Foods.CreateFood;
using FitRos.Application.Features.Foods.UpdateFood;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Nutrition;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.Foods;

public class FoodsApiTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid OtherGymId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public FoodsApiTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static CreateFoodCommand NewFood(string? name = null) => new(
        name ?? $"Rice {Guid.NewGuid()}",
        FoodCategory.Carbohydrate,
        130m, 2.7m, 28m, 0.3m, null);

    private async Task<Guid> PlantForeignFoodAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();
        var food = Food.Create($"Foreign {Guid.NewGuid()}", FoodCategory.Protein,
            100m, 20m, 0m, 5m, null, OtherGymId);
        db.Foods.Add(food);
        await db.SaveChangesAsync(CancellationToken.None);
        return food.Id;
    }

    // ---------- happy paths ----------

    [Fact]
    public async Task Create_then_get_by_id_round_trips()
    {
        var create = await _client.PostAsJsonAsync("/api/foods", NewFood());
        create.StatusCode.Should().Be(HttpStatusCode.Created,
            await create.Content.ReadAsStringAsync());

        var created = await create.Content.ReadFromJsonAsync<CreateFoodResponse>();
        var get = await _client.GetAsync($"/api/foods/{created!.Id}");

        get.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task List_returns_200()
    {
        var response = await _client.GetAsync("/api/foods");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_with_blank_name_is_400()
    {
        var response = await _client.PostAsJsonAsync("/api/foods", NewFood(name: ""));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- auth ----------

    [Fact]
    public async Task Unauthenticated_list_is_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/foods");
        request.Headers.Add(TestAuthHandler.UnauthenticatedHeader, "1");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- tenant isolation ----------

    [Fact]
    public async Task Get_by_id_of_a_foreign_gyms_food_is_404()
    {
        var id = await PlantForeignFoodAsync();

        var response = await _client.GetAsync($"/api/foods/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_of_a_foreign_gyms_food_is_404()
    {
        var id = await PlantForeignFoodAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/foods/{id}",
            new UpdateFoodCommand(id, "Hijacked", FoodCategory.Protein, 1, 1, 1, 1, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Archive_of_a_foreign_gyms_food_is_404()
    {
        var id = await PlantForeignFoodAsync();

        var response = await _client.DeleteAsync($"/api/foods/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Foreign_gyms_food_never_appears_in_the_list()
    {
        var id = await PlantForeignFoodAsync();

        var foods = await _client.GetFromJsonAsync<List<FoodRow>>("/api/foods?includeArchived=true");

        foods!.Select(f => f.Id).Should().NotContain(id);
    }

    private sealed record FoodRow(Guid Id);
}
