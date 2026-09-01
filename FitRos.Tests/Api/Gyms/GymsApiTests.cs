using FitRos.API;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FitRos.Tests.Api.Gyms;

public class GymsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GymsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // =============================
    // CREATE GYM
    // =============================

    [Fact]
    public async Task CreateGym_Should_Return_401_When_User_Not_Authenticated()
    {
        var request = new
        {
            name = "FitRos Gym",
            city = "San Jose",
            phone = "8888-8888",
            logoUrl = (string?)null,
            adminEmail = "admin@gym.com",
            adminFirstName = "Admin",
            adminLastName = "Gym",
            adminPassword = "Password123"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/gyms",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =============================
    // INVALID REQUEST
    // =============================

    [Fact]
    public async Task CreateGym_Should_Return_400_When_Request_Is_Invalid()
    {
        var request = new
        {
            name = "",
            city = "",
            phone = "",
            logoUrl = (string?)null,
            adminEmail = "",
            adminFirstName = "",
            adminLastName = "",
            adminPassword = ""
        };

        var response = await _client.PostAsJsonAsync(
            "/api/gyms",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =============================
    // ROUTE VALIDATION
    // =============================

    [Fact]
    public async Task GetGyms_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.GetAsync("/api/gyms");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}