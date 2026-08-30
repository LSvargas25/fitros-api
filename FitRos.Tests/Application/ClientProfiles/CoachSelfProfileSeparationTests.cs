using FitRos.Application.Features.ClientProfiles.GetByCoach;
using FitRos.Application.Features.ClientProfiles.GetMyClient;
using FitRos.Application.Features.ClientProfiles.GetMyClients;
using FitRos.Application.Features.ClientProfiles.GetMyClientProfile;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles;

// A coach's personal ClientProfile (Kind = CoachSelf) must never appear in any
// client roster: not the coach's own list, not the admin's gym-wide list, not
// GetClientsByCoach. It IS resolvable by the coach via "/me".
public class CoachSelfProfileSeparationTests
{
    private static readonly Guid GymId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private sealed record Fixture(FitRosDbContext Ctx, Guid CoachUserId);

    private static Fixture Seed()
    {
        var seeder = new FakeCurrentUser(GymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };
        var ctx = TestDbContextFactory.Create(seeder);

        var coach = User.CreateForGym(GymId, "coach@sep.test", "The", "Coach", "h", UserRole.Coach);
        ctx.Users.Add(coach);

        // The coach's own personal profile.
        ctx.ClientProfiles.Add(ClientProfile.CreateForCoachSelf(GymId, coach.Id));

        // Two real clients this coach manages.
        for (var i = 0; i < 2; i++)
        {
            var clientUser = User.CreateForGym(GymId, $"client{i}@sep.test", "Client", $"{i}", "h", UserRole.Client);
            ctx.Users.Add(clientUser);
            ctx.ClientProfiles.Add(ClientProfile.Create(GymId, clientUser.Id, coach.Id));
        }

        ctx.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
        return new Fixture(ctx, coach.Id);
    }

    private static FakeCurrentUser Caller(UserRole role, Guid userId) => new(GymId)
    {
        UserId = userId,
        Role = role,
        IsAuthenticated = true
    };

    [Fact]
    public async Task Coach_own_client_list_excludes_their_personal_profile()
    {
        var (ctx, coachUserId) = Seed();
        var handler = new GetMyClientsQueryHandler(ctx, Caller(UserRole.Coach, coachUserId));

        var result = await handler.Handle(new GetMyClientsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().NotContain(r => r.ClientUserId == coachUserId);
    }

    [Fact]
    public async Task Admin_gym_client_list_excludes_the_coachs_personal_profile()
    {
        var (ctx, coachUserId) = Seed();
        var handler = new GetMyClientsQueryHandler(ctx, Caller(UserRole.Admin, Guid.NewGuid()));

        var result = await handler.Handle(new GetMyClientsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.ClientUserId != coachUserId);
    }

    [Fact]
    public async Task GetClientsByCoach_excludes_the_coachs_personal_profile()
    {
        var (ctx, coachUserId) = Seed();
        var handler = new GetClientsByCoachQueryHandler(ctx, Caller(UserRole.Admin, Guid.NewGuid()));

        var result = await handler.Handle(new GetClientsByCoachQuery(coachUserId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().NotContain(r => r.UserId == coachUserId);
    }

    [Fact]
    public async Task Coach_can_resolve_their_own_personal_profile_via_me()
    {
        var (ctx, coachUserId) = Seed();
        var handler = new GetMyClientProfileQueryHandler(ctx, Caller(UserRole.Coach, coachUserId));

        var result = await handler.Handle(new GetMyClientProfileQuery(), CancellationToken.None);

        result.UserId.Should().Be(coachUserId);
    }
}
