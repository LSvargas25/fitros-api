using System.Net;
using System.Net.Http.Json;
using FitRos.API.Contracts.ClientProfiles;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Entities.Client;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.ClientProfiles;

// Authenticated (Admin, gym 11111111-…). The existing ClientProfilesApiTests
// only assert 401. This covers a happy path plus every cross-gym rule
// (measures / KPI snapshots / get-by-id / status / soft-delete) — all now
// gym-scoped for Admin after BACKEND_AUDIT 11.10 was resolved.
public class ClientProfilesTenantTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid TestGymId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherGymId = Guid.Parse("22221111-1111-1111-1111-111111119999");

    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public ClientProfilesTenantTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> PlantProfileAsync(Guid gymId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();
        var profile = ClientProfile.Create(gymId, Guid.NewGuid(), coachId: Guid.NewGuid());
        db.ClientProfiles.Add(profile);
        await db.SaveChangesAsync(CancellationToken.None);
        return profile.Id;
    }

    private static AddPhysicalMeasureRequest SampleMeasure() => new()
    {
        Weight = 80m, BodyFatPercentage = 18m, MuscleMass = 40m,
        Waist = 85m, Chest = 100m, Arms = 35m
    };

    [Fact]
    public async Task Add_and_list_measures_for_an_own_gym_client()
    {
        var id = await PlantProfileAsync(TestGymId);

        var add = await _client.PostAsJsonAsync($"/api/client-profiles/{id}/measures", SampleMeasure());
        add.StatusCode.Should().Be(HttpStatusCode.NoContent, await add.Content.ReadAsStringAsync());

        var list = await _client.GetAsync($"/api/client-profiles/{id}/measures");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Adding_a_measure_to_a_client_in_another_gym_is_forbidden()
    {
        var id = await PlantProfileAsync(OtherGymId);

        var response = await _client.PostAsJsonAsync(
            $"/api/client-profiles/{id}/measures", SampleMeasure());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Listing_measures_of_a_client_in_another_gym_is_forbidden()
    {
        var id = await PlantProfileAsync(OtherGymId);

        var response = await _client.GetAsync($"/api/client-profiles/{id}/measures");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reading_KPI_snapshots_of_a_client_in_another_gym_is_forbidden()
    {
        var id = await PlantProfileAsync(OtherGymId);

        var response = await _client.GetAsync($"/api/client-profiles/{id}/kpi-snapshots");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Generating_a_KPI_snapshot_for_a_client_in_another_gym_is_forbidden()
    {
        var id = await PlantProfileAsync(OtherGymId);

        var response = await _client.PostAsync($"/api/client-profiles/{id}/kpi-snapshot", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reading_a_client_profile_in_another_gym_is_forbidden()
    {
        var id = await PlantProfileAsync(OtherGymId);

        var response = await _client.GetAsync($"/api/client-profiles/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Changing_status_of_a_client_in_another_gym_is_forbidden()
    {
        var id = await PlantProfileAsync(OtherGymId);

        var response = await _client.PatchAsJsonAsync(
            $"/api/client-profiles/{id}/status", new { activate = false });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Soft_deleting_a_client_in_another_gym_is_forbidden()
    {
        var id = await PlantProfileAsync(OtherGymId);

        var response = await _client.DeleteAsync($"/api/client-profiles/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
