using FitRos.Application.Features.Users.UserManagement.CreateUser;
using FitRos.Application.Features.Users.Client.CreateClient;
using FitRos.Application.Features.Users.Coach.CreateCoach;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FitRos.Tests.Api.Users;

public class UsersApiTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public UsersApiTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateClient_Should_Return_201()
    {
        var request = new CreateClientCommand(
            $"{Guid.NewGuid()}@test.com",
            "John",
            "Doe",
            "Password123!"
        );

        var response = await _client.PostAsJsonAsync(
            "/api/clients",
            request);

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
    }

    [Fact]
    public async Task CreateCoach_Should_Return_201()
    {
        var request = new CreateCoachCommand(
            $"{Guid.NewGuid()}@test.com",
            "Jane",
            "Smith",
            "Password123!"
        );

        var response = await _client.PostAsJsonAsync(
            "/api/coaches",
            request);

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
    }
    [Fact]
    public async Task GetById_Should_Return_200_When_User_Exists()
    {
        var createRequest = new CreateClientCommand(
            $"{Guid.NewGuid()}@test.com",
            "Mike",
            "Jordan",
            "Password123!"
        );

        var createResponse = await _client.PostAsJsonAsync(
            "/api/clients",
            createRequest);

        // deserialize FIRST, then check status
        var created = await createResponse.Content
            .ReadFromJsonAsync<CreateUserResponse>();

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Should().NotBeNull("create request failed");

        var response = await _client.GetAsync($"/api/users/{created!.Id}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
    }

    [Fact]
    public async Task Deactivate_Should_Return_204()
    {
        var createRequest = new CreateClientCommand(
            $"{Guid.NewGuid()}@test.com",
            "Alex",
            "Brown",
            "Password123!"
        );

        var createResponse = await _client.PostAsJsonAsync(
            "/api/clients",
            createRequest);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CreateUserResponse>();

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Should().NotBeNull("create request failed");

        var response = await _client.DeleteAsync($"/api/users/{created!.Id}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }

    [Fact]
    public async Task Activate_Should_Return_204()
    {
        var createRequest = new CreateClientCommand(
            $"{Guid.NewGuid()}@test.com",
            "Robert",
            "Taylor",
            "Password123!"
        );

        var createResponse = await _client.PostAsJsonAsync(
            "/api/clients",
            createRequest);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CreateUserResponse>();

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Should().NotBeNull("create request failed");

        var deactivateResponse = await _client.DeleteAsync($"/api/users/{created!.Id}");
        var deactivateBody = await deactivateResponse.Content.ReadAsStringAsync();
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, deactivateBody);

        var response = await _client.PatchAsync(
            $"/api/users/{created!.Id}/activate",
            null);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }
    [Fact]
    public async Task GetUsersAdvanced_Should_Return_200()
    {
        var response = await _client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------- authz / tenant ----------

    [Fact]
    public async Task GetById_Unauthenticated_Is_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{Guid.NewGuid()}");
        request.Headers.Add(TestAuthHandler.UnauthenticatedHeader, "1");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Cross-gym behaviour for these endpoints is covered at the handler level in
    // UserManagementTenantTests — planting a User straight into the WAF store for
    // an HTTP round trip doesn't work reliably here (the seeded-owner HasData
    // model makes the WAF's request-side Users store diverge from a manually
    // created scope), so it's asserted in-process instead.
}
