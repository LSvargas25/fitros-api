using FitRos.API;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace FitRos.Tests.Api.Notifications;

public class NotificationsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public NotificationsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetNotifications_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUnread_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.GetAsync("/api/notifications/unread");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkAsRead_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.PatchAsync(
            $"/api/notifications/{Guid.NewGuid()}/read",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.DeleteAsync(
            $"/api/notifications/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
