using System.Net;
using System.Net.Http.Json;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Api.Gyms;

// TestApiFactory authenticates as an Admin in gym 11111111-…. Update / activate
// / deactivate now reject any gym id that isn't the admin's own.
public class GymsAdminScopeTests : IClassFixture<TestApiFactory>
{
    private const string OwnGymId = "11111111-1111-1111-1111-111111111111";
    private static readonly Guid OtherGymId = Guid.Parse("aaaabbbb-cccc-dddd-eeee-ffff00001111");

    private readonly HttpClient _client;

    public GymsAdminScopeTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Admin_cannot_update_another_gym()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/gyms/{OtherGymId}",
            new { name = "Hijacked", address = "x", phoneNumber = "123" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_cannot_activate_another_gym()
    {
        var response = await _client.PatchAsync($"/api/gyms/{OtherGymId}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_cannot_deactivate_another_gym()
    {
        var response = await _client.PatchAsync($"/api/gyms/{OtherGymId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_passes_the_authz_gate_for_their_own_gym()
    {
        // Own gym isn't seeded in the store, so this is a 400 ("Gym not found"),
        // but crucially NOT a 403 — the gym-scope check let it through.
        var response = await _client.PutAsJsonAsync(
            $"/api/gyms/{OwnGymId}",
            new { name = "My Gym", address = "x", phoneNumber = "123" });

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
