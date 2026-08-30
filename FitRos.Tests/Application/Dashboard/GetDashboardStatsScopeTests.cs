using FitRos.Application.Features.Dashboard.Queries.GetDashboardStats;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Dashboard;

public class GetDashboardStatsScopeTests
{
    // Gym A: 1 admin, 2 coaches, 3 live clients (+1 soft-deleted), 2 routines
    // Gym B: 1 admin, 1 coach, 1 live client, 1 routine

    private static User Staff(Guid gymId, UserRole role, string tag) =>
        User.CreateForGym(gymId, $"{tag}@fitros.test", tag, "User", "hash", role);

    private static FitRosDbContext BuildFixture()
    {
        var seeder = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };
        var ctx = TestDbContextFactory.Create(seeder);

        var gymA = Gym.Create("Gym A", "1 A St", "111");
        var gymB = Gym.Create("Gym B", "2 B St", "222");
        ctx.Gyms.AddRange(gymA, gymB);

        // Gym A staff / clients / routines
        ctx.Users.Add(Staff(gymA.Id, UserRole.Admin, "a-admin"));
        ctx.Users.Add(Staff(gymA.Id, UserRole.Coach, "a-coach1"));
        ctx.Users.Add(Staff(gymA.Id, UserRole.Coach, "a-coach2"));

        ctx.ClientProfiles.Add(ClientProfile.Create(gymA.Id, Guid.NewGuid()));
        ctx.ClientProfiles.Add(ClientProfile.Create(gymA.Id, Guid.NewGuid()));
        ctx.ClientProfiles.Add(ClientProfile.Create(gymA.Id, Guid.NewGuid()));

        var deletedClient = ClientProfile.Create(gymA.Id, Guid.NewGuid());
        deletedClient.SoftDelete();
        ctx.ClientProfiles.Add(deletedClient);

        ctx.WorkoutRoutines.Add(WorkoutRoutine.Create(gymA.Id, "A Routine 1", "x"));
        ctx.WorkoutRoutines.Add(WorkoutRoutine.Create(gymA.Id, "A Routine 2", "x"));

        // Gym B staff / clients / routines
        ctx.Users.Add(Staff(gymB.Id, UserRole.Admin, "b-admin"));
        ctx.Users.Add(Staff(gymB.Id, UserRole.Coach, "b-coach1"));

        ctx.ClientProfiles.Add(ClientProfile.Create(gymB.Id, Guid.NewGuid()));

        ctx.WorkoutRoutines.Add(WorkoutRoutine.Create(gymB.Id, "B Routine 1", "x"));

        ctx.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();

        // Hand back the actual ids the fixture used.
        _lastGymAId = gymA.Id;
        _lastGymBId = gymB.Id;
        return ctx;
    }

    private static Guid _lastGymAId;
    private static Guid _lastGymBId;

    private static GetDashboardStatsHandler HandlerFor(FitRosDbContext ctx, UserRole role, Guid? gymId)
    {
        var caller = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = role,
            IsAuthenticated = true
        };
        return new GetDashboardStatsHandler(ctx, caller);
    }

    [Fact]
    public async Task OwnerApp_sees_platform_wide_totals()
    {
        var ctx = BuildFixture();
        var handler = HandlerFor(ctx, UserRole.OwnerApp, null);

        var result = await handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        result.TotalGyms.Should().Be(2);
        result.TotalAdmins.Should().Be(2);
        result.TotalCoaches.Should().Be(3);
        result.TotalClients.Should().Be(4);   // soft-deleted client excluded
        result.TotalRoutines.Should().Be(3);
        result.Gyms.Should().HaveCount(2);

        var a = result.Gyms.Single(g => g.Id == _lastGymAId);
        a.ClientCount.Should().Be(3);
        a.CoachCount.Should().Be(2);
        a.AdminCount.Should().Be(1);

        var b = result.Gyms.Single(g => g.Id == _lastGymBId);
        b.ClientCount.Should().Be(1);
        b.CoachCount.Should().Be(1);
        b.AdminCount.Should().Be(1);
    }

    [Fact]
    public async Task Admin_sees_only_their_own_gym()
    {
        var ctx = BuildFixture();
        var handler = HandlerFor(ctx, UserRole.Admin, _lastGymAId);

        var result = await handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        result.TotalGyms.Should().Be(1);
        result.TotalAdmins.Should().Be(1);
        result.TotalCoaches.Should().Be(2);
        result.TotalClients.Should().Be(3);
        result.TotalRoutines.Should().Be(2);
        result.Gyms.Should().ContainSingle()
            .Which.Id.Should().Be(_lastGymAId);
        result.Gyms.Single().CoachCount.Should().Be(2);
        result.Gyms.Single().AdminCount.Should().Be(1);
        result.Gyms.Single().ClientCount.Should().Be(3);
    }

    [Fact]
    public async Task Coach_sees_the_same_gym_scope_as_an_Admin()
    {
        var ctx = BuildFixture();
        var admin = await HandlerFor(ctx, UserRole.Admin, _lastGymAId)
            .Handle(new GetDashboardStatsQuery(), CancellationToken.None);
        var coach = await HandlerFor(ctx, UserRole.Coach, _lastGymAId)
            .Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        coach.Should().BeEquivalentTo(admin);
    }

    [Fact]
    public async Task Admin_from_gym_B_never_sees_gym_A_numbers()
    {
        var ctx = BuildFixture();
        var handler = HandlerFor(ctx, UserRole.Admin, _lastGymBId);

        var result = await handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        result.TotalClients.Should().Be(1);
        result.TotalCoaches.Should().Be(1);
        result.Gyms.Should().ContainSingle().Which.Id.Should().Be(_lastGymBId);
        result.Gyms.Single().Name.Should().Be("Gym B");
    }

    [Fact]
    public async Task Admin_with_no_gym_gets_an_empty_dashboard_not_an_error()
    {
        var ctx = BuildFixture();
        var handler = HandlerFor(ctx, UserRole.Admin, gymId: null);

        var result = await handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        result.TotalGyms.Should().Be(0);
        result.TotalAdmins.Should().Be(0);
        result.TotalCoaches.Should().Be(0);
        result.TotalClients.Should().Be(0);
        result.TotalRoutines.Should().Be(0);
        result.Gyms.Should().BeEmpty();
    }
}
