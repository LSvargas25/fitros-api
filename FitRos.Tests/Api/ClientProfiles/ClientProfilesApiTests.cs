using FitRos.API;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FitRos.Tests.Api.ClientProfiles;

public sealed class ClientProfilesApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ClientProfilesApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMyClients_Should_Return_401_When_User_Is_Not_Authenticated()
    {
        var response = await _client.GetAsync("/api/client-profiles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetClientById_Should_Return_401_When_User_Is_Not_Authenticated()
    {
        var id = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/client-profiles/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyClient_Should_Return_401_When_User_Is_Not_Authenticated()
    {
        var response = await _client.GetAsync("/api/client-profiles/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetClientsByCoach_Should_Return_401_When_User_Is_Not_Authenticated()
    {
        var coachId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/client-profiles/by-coach/{coachId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddPhysicalMeasure_Should_Return_401_When_User_Is_Not_Authenticated()
    {
        var clientId = Guid.NewGuid();

        var request = new
        {
            weight = 80,
            bodyFatPercentage = 12,
            muscleMass = 40,
            waist = 80,
            chest = 100,
            arms = 35
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/client-profiles/{clientId}/measures",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangeStatus_Should_Return_401_When_User_Is_Not_Authenticated()
    {
        var clientId = Guid.NewGuid();

        var request = new
        {
            activate = true
        };

        var response = await _client.PatchAsJsonAsync(
            $"/api/client-profiles/{clientId}/status",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReassignCoach_Should_Return_401_When_User_Is_Not_Authenticated()
    {
        var clientId = Guid.NewGuid();

        var request = new
        {
            newCoachId = Guid.NewGuid()
        };

        var response = await _client.PatchAsJsonAsync(
            $"/api/client-profiles/{clientId}/reassign",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SoftDelete_Should_Return_401_When_User_Is_Not_Authenticated()
    {
        var clientId = Guid.NewGuid();

        var response = await _client.DeleteAsync(
            $"/api/client-profiles/{clientId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HardDelete_Should_Return_401_When_User_Is_Not_Authenticated()
    {
        var clientId = Guid.NewGuid();

        var response = await _client.DeleteAsync(
            $"/api/client-profiles/{clientId}/hard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}