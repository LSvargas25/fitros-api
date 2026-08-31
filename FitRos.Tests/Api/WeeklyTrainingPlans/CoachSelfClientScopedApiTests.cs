using System.Net;
using System.Net.Http.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Entities.Client;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.WeeklyTrainingPlans;

// BACKEND_AUDIT §25 — end-to-end over HTTP for the originally reported bug:
// a Coach hitting the client-scoped training/meal reads for their OWN CoachSelf
// profile id must not get 403; another profile's id still does.
public class CoachSelfClientScopedApiTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid TestGymId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MeUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly TestApiFactory _factory;
    private readonly HttpClient _coach;

    public CoachSelfClientScopedApiTests(TestApiFactory factory)
    {
        _factory = factory;
        _coach = factory.CreateClient();
        _coach.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Coach");
    }

    private (Guid mine, Guid someoneElse) Seed()
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();

        var mine = ClientProfile.CreateForCoachSelf(TestGymId, MeUserId);           // CoachId == null
        var someoneElse = ClientProfile.Create(TestGymId, Guid.NewGuid(), Guid.NewGuid());
        db.ClientProfiles.AddRange(mine, someoneElse);
        db.SaveChanges();
        return (mine.Id, someoneElse.Id);
    }

    [Theory]
    [InlineData("/api/training-plans/client/{id}")]
    [InlineData("/api/training-plans/client/{id}/active")]
    [InlineData("/api/meal-plans/client/{id}")]
    [InlineData("/api/meal-plans/client/{id}/active")]
    public async Task Coach_reading_their_own_CoachSelf_profile_is_not_forbidden(string template)
    {
        var (mine, _) = Seed();

        var res = await _coach.GetAsync(template.Replace("{id}", mine.ToString()));

        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        res.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/api/training-plans/client/{id}")]
    [InlineData("/api/training-plans/client/{id}/active")]
    [InlineData("/api/meal-plans/client/{id}")]
    [InlineData("/api/meal-plans/client/{id}/active")]
    public async Task Coach_reading_another_profile_is_still_forbidden(string template)
    {
        var (_, someoneElse) = Seed();

        var res = await _coach.GetAsync(template.Replace("{id}", someoneElse.ToString()));

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
