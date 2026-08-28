using System.Net;
using System.Net.Http.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.Users.Client.CreateClient;
using FitRos.Application.Features.Users.Client.UpdateClient;
using FitRos.Application.Features.Users.UserManagement.CreateUser;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.Clients;

public class ClientsApiTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid OtherGymId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public ClientsApiTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static CreateClientCommand NewClient(string? email = null) => new(
        email ?? $"client{Guid.NewGuid():N}@fitros.com",
        "Ana", "Diaz", "secret1");

    private async Task<Guid> PlantForeignClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();

        var user = User.CreateForGym(
            OtherGymId, $"foreign{Guid.NewGuid():N}@fitros.com",
            "Foreign", "Client", "hash", UserRole.Client);
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);
        return user.Id;
    }

    // ---------- happy paths ----------

    [Fact]
    public async Task Create_returns_201()
    {
        var response = await _client.PostAsJsonAsync("/api/clients", NewClient());

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task List_returns_200()
    {
        var response = await _client.GetAsync("/api/clients");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_then_get_by_id_round_trips()
    {
        var create = await _client.PostAsJsonAsync("/api/clients", NewClient());
        var created = await create.Content.ReadFromJsonAsync<CreateUserResponse>();

        var get = await _client.GetAsync($"/api/clients/{created!.Id}");

        get.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_uses_the_route_id_and_does_not_need_it_in_the_body()
    {
        var create = await _client.PostAsJsonAsync("/api/clients", NewClient());
        var created = await create.Content.ReadFromJsonAsync<CreateUserResponse>();

        // Body carries only the editable fields — no clientId.
        var update = await _client.PutAsJsonAsync(
            $"/api/clients/{created!.Id}",
            new { firstName = "Renamed", lastName = "Client" });

        update.StatusCode.Should().Be(HttpStatusCode.OK, await update.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_with_a_bad_email_is_400()
    {
        var response = await _client.PostAsJsonAsync("/api/clients", NewClient(email: "not-an-email"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- auth ----------

    [Fact]
    public async Task Unauthenticated_list_is_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/clients");
        request.Headers.Add(TestAuthHandler.UnauthenticatedHeader, "1");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- tenant isolation ----------

    [Fact]
    public async Task Get_by_id_of_a_client_in_another_gym_is_denied()
    {
        var id = await PlantForeignClientAsync();

        var response = await _client.GetAsync($"/api/clients/{id}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_of_a_client_in_another_gym_is_denied()
    {
        var id = await PlantForeignClientAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/clients/{id}",
            new UpdateClientCommand(id, "Hijacked", "Name", null));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_of_a_client_in_another_gym_is_denied()
    {
        var id = await PlantForeignClientAsync();

        var response = await _client.DeleteAsync($"/api/clients/{id}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }
}
