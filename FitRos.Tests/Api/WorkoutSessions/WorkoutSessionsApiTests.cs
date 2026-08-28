using System.Net;
using System.Net.Http.Json;
using FitRos.API.Contracts.WorkoutSessions;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Entities.Training;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.WorkoutSessions;

public class WorkoutSessionsApiTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid TestGymId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherGymId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public WorkoutSessionsApiTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> PlantSessionAsync(Guid gymId, Guid userId, bool started)
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();

        var session = WorkoutSession.Create(
            gymId, userId, Guid.NewGuid(), "Plan", 1, DateTime.UtcNow.Date);
        if (started)
            session.Start();

        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync(CancellationToken.None);
        return session.Id;
    }

    // ---------- auth ----------

    [Theory]
    [InlineData("GET", "/api/workout-sessions/history")]
    [InlineData("GET", "/api/workout-sessions/00000000-0000-0000-0000-000000000001")]
    public async Task Unauthenticated_requests_are_401(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Add(TestAuthHandler.UnauthenticatedHeader, "1");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- basic behaviour ----------

    [Fact]
    public async Task Get_by_id_of_a_nonexistent_session_is_404()
    {
        var response = await _client.GetAsync($"/api/workout-sessions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task History_returns_200()
    {
        var response = await _client.GetAsync("/api/workout-sessions/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Own_in_progress_session_can_be_read_get_a_set_and_be_completed()
    {
        var id = await PlantSessionAsync(TestGymId, TestUserId, started: true);

        var get = await _client.GetAsync($"/api/workout-sessions/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK, await get.Content.ReadAsStringAsync());

        var addSet = await _client.PostAsJsonAsync(
            $"/api/workout-sessions/{id}/sets",
            new AddSetRequest(Guid.NewGuid(), 1, 10, 40m));
        addSet.StatusCode.Should().Be(HttpStatusCode.NoContent, await addSet.Content.ReadAsStringAsync());

        var complete = await _client.PatchAsync($"/api/workout-sessions/{id}/complete", null);
        complete.StatusCode.Should().Be(HttpStatusCode.NoContent, await complete.Content.ReadAsStringAsync());
    }

    // ---------- tenant isolation ----------

    [Fact]
    public async Task Get_by_id_of_a_foreign_gyms_session_is_denied()
    {
        var id = await PlantSessionAsync(OtherGymId, Guid.NewGuid(), started: true);

        var response = await _client.GetAsync($"/api/workout-sessions/{id}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Completing_a_foreign_gyms_session_is_denied()
    {
        var id = await PlantSessionAsync(OtherGymId, Guid.NewGuid(), started: true);

        var response = await _client.PatchAsync($"/api/workout-sessions/{id}/complete", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Skipping_a_foreign_gyms_session_is_denied()
    {
        var id = await PlantSessionAsync(OtherGymId, Guid.NewGuid(), started: true);

        var response = await _client.PatchAsync($"/api/workout-sessions/{id}/skip", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Adding_a_set_to_a_foreign_gyms_session_is_denied()
    {
        var id = await PlantSessionAsync(OtherGymId, Guid.NewGuid(), started: true);

        var response = await _client.PostAsJsonAsync(
            $"/api/workout-sessions/{id}/sets",
            new AddSetRequest(Guid.NewGuid(), 1, 10, 40m));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }
}
