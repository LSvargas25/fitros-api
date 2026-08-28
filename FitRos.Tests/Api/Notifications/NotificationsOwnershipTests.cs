using System.Net;
using System.Net.Http.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Notifications;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.Notifications;

// Authenticated as user 22222222-… in gym 11111111-…. Notifications are
// per-recipient: another user (even same gym) must not touch mine; a
// notification from another gym is invisible (tenant filter).
public class NotificationsOwnershipTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid MeUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TestGymId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherGymId = Guid.Parse("33339999-3333-9999-3333-999933339999");

    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public NotificationsOwnershipTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> PlantNotificationAsync(Guid userId, Guid? gymId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();
        var n = Notification.Create(userId, gymId, "T", "M", NotificationType.RoutineCreated);
        db.Notifications.Add(n);
        await db.SaveChangesAsync(CancellationToken.None);
        return n.Id;
    }

    [Fact]
    public async Task List_only_returns_the_callers_own_notifications()
    {
        var mine = await PlantNotificationAsync(MeUserId, TestGymId);
        var someoneElses = await PlantNotificationAsync(Guid.NewGuid(), TestGymId);

        var rows = await _client.GetFromJsonAsync<List<Row>>("/api/notifications");

        rows!.Select(r => r.Id).Should().Contain(mine).And.NotContain(someoneElses);
    }

    [Fact]
    public async Task Mark_own_notification_as_read_then_delete_it()
    {
        var id = await PlantNotificationAsync(MeUserId, TestGymId);

        var read = await _client.PatchAsync($"/api/notifications/{id}/read", null);
        read.StatusCode.Should().Be(HttpStatusCode.NoContent, await read.Content.ReadAsStringAsync());

        var del = await _client.DeleteAsync($"/api/notifications/{id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Marking_another_users_notification_as_read_is_403()
    {
        var id = await PlantNotificationAsync(Guid.NewGuid(), TestGymId);

        var response = await _client.PatchAsync($"/api/notifications/{id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deleting_another_users_notification_is_403()
    {
        var id = await PlantNotificationAsync(Guid.NewGuid(), TestGymId);

        var response = await _client.DeleteAsync($"/api/notifications/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_notification_from_another_gym_is_not_found()
    {
        var id = await PlantNotificationAsync(MeUserId, OtherGymId);

        var response = await _client.PatchAsync($"/api/notifications/{id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record Row(Guid Id);
}
