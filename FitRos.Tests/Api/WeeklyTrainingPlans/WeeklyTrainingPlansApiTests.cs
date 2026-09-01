using FitRos.API;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FitRos.Tests.Api.WeeklyTrainingPlans;

public class WeeklyTrainingPlansApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WeeklyTrainingPlansApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.PostAsJsonAsync("/api/training-plans", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.GetAsync($"/api/training-plans/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetClientPlans_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.GetAsync($"/api/training-plans/client/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetActivePlan_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.GetAsync($"/api/training-plans/client/{Guid.NewGuid()}/active");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Rename_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.PatchAsync(
            $"/api/training-plans/{Guid.NewGuid()}/rename",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Activate_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.PatchAsync(
            $"/api/training-plans/{Guid.NewGuid()}/activate",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Archive_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.PatchAsync(
            $"/api/training-plans/{Guid.NewGuid()}/archive",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssignRoutineToDay_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/training-plans/{Guid.NewGuid()}/days/1",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveRoutineFromDay_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.DeleteAsync(
            $"/api/training-plans/{Guid.NewGuid()}/days/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
