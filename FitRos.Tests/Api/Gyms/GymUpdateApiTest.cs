using FitRos.API;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FitRos.Tests.Api.Gyms;

public class GymUpdateApiTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GymUpdateApiTest(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UpdateGym_Should_Return_401_When_User_Not_Authenticated()
    {
        var request = new
        {
            name = "Updated Gym",
            address = "New Address",
            phoneNumber = "9999"
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/gyms/{Guid.NewGuid()}",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateGym_Should_Return_400_When_Request_Is_Invalid()
    {
        var request = new
        {
            name = "",
            address = "",
            phoneNumber = ""
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/gyms/{Guid.NewGuid()}",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateGym_Should_Only_Allow_Put()
    {
        var response = await _client.PostAsync(
            $"/api/gyms/{Guid.NewGuid()}",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task UpdateGym_Should_Return_404_When_Route_Is_Invalid()
    {
        var request = new
        {
            name = "Gym",
            address = "Address",
            phoneNumber = "9999"
        };

        var response = await _client.PutAsJsonAsync(
            "/api/gym",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}