using System.Net;
using System.Net.Http.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.Exercises.CreateExercise;
using FitRos.Application.Features.Exercises.UpdateExercise;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.Exercises;

public class ExercisesTenantIsolationTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid OtherGymId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public ExercisesTenantIsolationTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> PlantForeignExerciseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();
        var exercise = Exercise.Create($"Foreign {Guid.NewGuid()}", "gym B only",
            MuscleGroup.Back, OtherGymId);
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync(CancellationToken.None);
        return exercise.Id;
    }

    [Fact]
    public async Task Unauthenticated_list_is_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/exercises");
        request.Headers.Add(TestAuthHandler.UnauthenticatedHeader, "1");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_with_blank_name_is_400()
    {
        var response = await _client.PostAsJsonAsync("/api/exercises",
            new CreateExerciseCommand("", "desc", MuscleGroup.Chest));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_update_archive_round_trips_within_the_own_gym()
    {
        var create = await _client.PostAsJsonAsync("/api/exercises",
            new CreateExerciseCommand($"Row {Guid.NewGuid()}", "desc", MuscleGroup.Back));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<CreateExerciseResponse>();

        var update = await _client.PutAsJsonAsync($"/api/exercises/{created!.Id}",
            new UpdateExerciseCommand(created.Id, "Barbell Row", "updated", MuscleGroup.Back));
        update.StatusCode.Should().Be(HttpStatusCode.NoContent, await update.Content.ReadAsStringAsync());

        var archive = await _client.DeleteAsync($"/api/exercises/{created.Id}");
        archive.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Get_by_id_of_a_foreign_gyms_exercise_is_404()
    {
        var id = await PlantForeignExerciseAsync();

        var response = await _client.GetAsync($"/api/exercises/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_of_a_foreign_gyms_exercise_is_404()
    {
        var id = await PlantForeignExerciseAsync();

        var response = await _client.PutAsJsonAsync($"/api/exercises/{id}",
            new UpdateExerciseCommand(id, "Hijacked", "x", MuscleGroup.Back));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Archive_of_a_foreign_gyms_exercise_is_404()
    {
        var id = await PlantForeignExerciseAsync();

        var response = await _client.DeleteAsync($"/api/exercises/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Foreign_gyms_exercise_never_appears_in_the_list()
    {
        var id = await PlantForeignExerciseAsync();

        var rows = await _client.GetFromJsonAsync<List<Row>>("/api/exercises");

        rows!.Select(r => r.Id).Should().NotContain(id);
    }

    private sealed record Row(Guid Id);
}
