using System.Net;
using System.Net.Http.Json;
using FitRos.API.Contracts.WorkoutRoutines;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.WorkoutRoutines.AddExerciseToWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.UpdateWorkoutRoutine;
using FitRos.Domain.Entities.Training;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.WorkoutRoutines;

// TestApiFactory authenticates as an Admin in gym 11111111-…. Here we plant a
// routine that belongs to a DIFFERENT gym and confirm every routine endpoint
// refuses to touch it (BACKEND_AUDIT 10.2 / frente 1 item 2).
public class WorkoutRoutineTenantIsolationTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid OtherGymId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public WorkoutRoutineTenantIsolationTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> PlantForeignRoutineAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();

        var routine = WorkoutRoutine.Create(OtherGymId, $"Foreign {Guid.NewGuid()}", "belongs to gym B");
        db.WorkoutRoutines.Add(routine);
        await db.SaveChangesAsync(CancellationToken.None);

        return routine.Id;
    }

    [Fact]
    public async Task Get_by_id_of_a_foreign_routine_is_denied()
    {
        var id = await PlantForeignRoutineAsync();

        var response = await _client.GetAsync($"/api/workoutroutines/{id}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_of_a_foreign_routine_is_denied()
    {
        var id = await PlantForeignRoutineAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/workoutroutines/{id}",
            new UpdateWorkoutRoutineCommand("Hijacked", "nope"));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Archive_of_a_foreign_routine_is_denied()
    {
        var id = await PlantForeignRoutineAsync();

        var response = await _client.DeleteAsync($"/api/workoutroutines/{id}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Publish_of_a_foreign_routine_is_denied()
    {
        var id = await PlantForeignRoutineAsync();

        var response = await _client.PutAsync($"/api/workoutroutines/{id}/publish", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Add_exercise_to_a_foreign_routine_is_denied()
    {
        var id = await PlantForeignRoutineAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/workoutroutines/{id}/exercises",
            new AddExerciseToWorkoutRoutineCommand(id, Guid.NewGuid(), 1, 3, 10, 60));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Move_exercise_in_a_foreign_routine_is_denied()
    {
        var id = await PlantForeignRoutineAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/workoutroutines/{id}/exercises/{Guid.NewGuid()}/move",
            new MoveExerciseRequest(1));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Remove_exercise_from_a_foreign_routine_is_denied()
    {
        var id = await PlantForeignRoutineAsync();

        var response = await _client.DeleteAsync(
            $"/api/workoutroutines/{id}/exercises/{Guid.NewGuid()}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Versions_of_a_foreign_routine_are_denied()
    {
        var id = await PlantForeignRoutineAsync();

        var response = await _client.GetAsync($"/api/workoutroutines/{id}/versions");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }
}
