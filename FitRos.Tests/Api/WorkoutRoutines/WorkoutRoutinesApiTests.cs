using FitRos.API.Contracts.WorkoutRoutines;
using FitRos.Application.Features.Exercises.CreateExercise;
using FitRos.Application.Features.WorkoutRoutines.AddExerciseToWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.UpdateWorkoutRoutine;
using FitRos.Domain.Entities.Enums;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FitRos.Tests.Api.WorkoutRoutines;

public class WorkoutRoutinesApiTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public WorkoutRoutinesApiTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // =============================
    // Helpers
    // =============================

    private async Task<Guid> CreateExerciseAsync()
    {
        var command = new CreateExerciseCommand(
            $"Exercise {Guid.NewGuid()}",
            "Test exercise",
            MuscleGroup.Chest
        );

        var response = await _client.PostAsJsonAsync("/api/exercises", command);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);

        var result = await response.Content
            .ReadFromJsonAsync<CreateExerciseResponse>();

        return result!.Id;
    }

    private async Task<Guid> CreateRoutineAsync(string name = "Test Routine")
    {
        var command = new CreateWorkoutRoutineCommand
        {
            Name = name,
            Description = "Routine description"
        };

        var response = await _client.PostAsJsonAsync("/api/workoutroutines", command);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);

        var result = await response.Content
            .ReadFromJsonAsync<CreateWorkoutRoutineResponse>();

        return result!.Id;
    }

    private async Task<Guid> CreatePublishedRoutineAsync(string name = "Published Routine")
    {
        var routineId = await CreateRoutineAsync(name);

        // Add exercise — required by domain rule before publishing
        var exerciseId = await CreateExerciseAsync();
        var addCommand = new AddExerciseToWorkoutRoutineCommand(
            routineId, exerciseId, 1, 3, 10, 60);
        var addResponse = await _client.PostAsJsonAsync(
            $"/api/workoutroutines/{routineId}/exercises", addCommand);
        var addBody = await addResponse.Content.ReadAsStringAsync();
        addResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, addBody);

        // Publish
        var publishResponse = await _client.PutAsync(
            $"/api/workoutroutines/{routineId}/publish", null);
        var publishBody = await publishResponse.Content.ReadAsStringAsync();
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, publishBody);

        return routineId;
    }

    // =============================
    // Tests
    // =============================

    [Fact]
    public async Task Get_Should_Return_200()
    {
        var response = await _client.GetAsync("/api/workoutroutines?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_Should_Return_404_When_Not_Found()
    {
        var response = await _client.GetAsync($"/api/workoutroutines/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Should_Return_201()
    {
        var command = new CreateWorkoutRoutineCommand
        {
            Name = $"Chest Routine {Guid.NewGuid()}",
            Description = "Chest workout"
        };

        var response = await _client.PostAsJsonAsync("/api/workoutroutines", command);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
    }

    [Fact]
    public async Task Update_Should_Return_204()
    {
        var routineId = await CreateRoutineAsync($"Routine {Guid.NewGuid()}");

        var command = new UpdateWorkoutRoutineCommand(
            "Updated Routine",
            "Updated description");

        var response = await _client.PutAsJsonAsync(
            $"/api/workoutroutines/{routineId}", command);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }

    [Fact]
    public async Task GetLatest_Should_Return_200()
    {
        var routineId = await CreateRoutineAsync($"Routine {Guid.NewGuid()}");

        var response = await _client.GetAsync(
            $"/api/workoutroutines/{routineId}/latest");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
    }

    [Fact]
    public async Task GetVersions_Should_Return_200()
    {
        var routineId = await CreateRoutineAsync($"Routine {Guid.NewGuid()}");

        var response = await _client.GetAsync(
            $"/api/workoutroutines/{routineId}/versions");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
    }

    [Fact]
    public async Task Publish_Should_Return_204()
    {
        var routineId = await CreateRoutineAsync($"Routine To Publish {Guid.NewGuid()}");

        var exerciseId = await CreateExerciseAsync();
        var addCommand = new AddExerciseToWorkoutRoutineCommand(
            routineId, exerciseId, 1, 3, 10, 60);
        var addResponse = await _client.PostAsJsonAsync(
            $"/api/workoutroutines/{routineId}/exercises", addCommand);
        var addBody = await addResponse.Content.ReadAsStringAsync();
        addResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, addBody);

        var response = await _client.PutAsync(
            $"/api/workoutroutines/{routineId}/publish", null);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }

    [Fact]
    public async Task CreateVersion_Should_Return_201()
    {
        var routineId = await CreatePublishedRoutineAsync(
            $"Routine {Guid.NewGuid()}");

        var response = await _client.PostAsync(
            $"/api/workoutroutines/{routineId}/versions", null);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
    }

    [Fact]
    public async Task Delete_Should_Return_204()
    {
        var routineId = await CreatePublishedRoutineAsync(
            $"Routine To Delete {Guid.NewGuid()}");

        var response = await _client.DeleteAsync(
            $"/api/workoutroutines/{routineId}");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }

    [Fact]
    public async Task AddExercise_Should_Return_400_When_Id_Mismatch()
    {
        var routeId = Guid.NewGuid();
        var commandId = Guid.NewGuid();

        var command = new AddExerciseToWorkoutRoutineCommand(
            commandId, Guid.NewGuid(), 1, 3, 10, 60);

        var response = await _client.PostAsJsonAsync(
            $"/api/workoutroutines/{routeId}/exercises", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MoveExercise_Should_Return_BadRequest_When_Routine_Not_Found()
    {
        // InMemory doesn't support transactions so we test the not-found path
        var routineId = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();

        var request = new MoveExerciseRequest(1);

        var response = await _client.PutAsJsonAsync(
            $"/api/workoutroutines/{routineId}/exercises/{exerciseId}/move",
            request);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
    }

    [Fact]
    public async Task RemoveExercise_Should_Return_BadRequest_When_Exercise_Not_Found()
    {
        // Domain rule: cannot remove exercise that doesn't exist in routine
        var routineId = await CreateRoutineAsync($"Routine {Guid.NewGuid()}");
        var exerciseId = Guid.NewGuid();

        var response = await _client.DeleteAsync(
            $"/api/workoutroutines/{routineId}/exercises/{exerciseId}");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
    }
}