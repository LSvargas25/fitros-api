using FitRos.API;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FitRos.Tests.Api.Dashboard;

public class DashboardApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DashboardApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // =============================
    // AUTH
    // =============================

    [Fact]
    public async Task GetDashboard_Should_Return_401_When_Not_Authenticated()
    {
        var response = await _client.GetAsync("/api/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =============================
    // METHOD VALIDATION
    // =============================

    [Fact]
    public async Task Dashboard_Should_Not_Allow_Post()
    {
        var response = await _client.PostAsJsonAsync("/api/dashboard", new { });

        // 405 is correct — method not allowed fires before auth
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Dashboard_Should_Not_Allow_Delete()
    {
        var response = await _client.DeleteAsync("/api/dashboard");

        // 405 is correct — method not allowed fires before auth
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}