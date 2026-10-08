using FitRos.Application.Features.Dashboard.Queries.GetDashboardStats;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;

namespace FitRos.Tests.Application.Dashboard;

public class GetDashboardStatsHandlerTests
{
    [Fact]
    public async Task Owner_gets_platform_wide_counts_despite_having_no_gym()
    {
        var owner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };
        var context = TestDbContextFactory.Create(owner);

        var gym = Gym.Create("Gym A", "City A", "1111-1111");
        context.Gyms.Add(gym);

        var admin = User.CreateForGym(gym.Id, "admin@a.com", "Ad", "Min", "hash", UserRole.Admin);
        var coach = User.CreateForGym(gym.Id, "coach@a.com", "Co", "Ach", "hash", UserRole.Coach);
        var inactiveCoach = User.CreateForGym(gym.Id, "old@a.com", "Old", "Coach", "hash", UserRole.Coach);
        inactiveCoach.Deactivate();
        var clientUser = User.CreateForGym(gym.Id, "client@a.com", "Cli", "Ent", "hash", UserRole.Client);
        var deletedUser = User.CreateForGym(gym.Id, "gone@a.com", "Gone", "Client", "hash", UserRole.Client);
        context.Users.AddRange(admin, coach, inactiveCoach, clientUser, deletedUser);

        var client = ClientProfile.Create(gym.Id, clientUser.Id, coach.Id);
        var deleted = ClientProfile.Create(gym.Id, deletedUser.Id, coach.Id);
        deleted.SoftDelete();
        context.ClientProfiles.AddRange(client, deleted);

        context.WorkoutRoutines.Add(WorkoutRoutine.Create(gym.Id, "Push", "Chest day"));

        await context.SaveChangesAsync();

        var result = await new GetDashboardStatsHandler(context, owner)
            .Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        result.TotalGyms.Should().Be(1);
        result.TotalAdmins.Should().Be(1);
        result.TotalCoaches.Should().Be(1);
        result.TotalClients.Should().Be(1);
        result.TotalRoutines.Should().Be(1);

        var summary = result.Gyms.Should().ContainSingle().Subject;
        summary.ClientCount.Should().Be(1);
        summary.CoachCount.Should().Be(1);
        summary.AdminCount.Should().Be(1);
    }

    [Fact]
    public async Task Admin_counts_stay_scoped_to_their_own_gym()
    {
        var container = TestDbContextFactory.CreateContainer(UserRole.Admin);
        var context = container.Context;
        var myGymId = container.CurrentUser.GymId!.Value;
        var otherGymId = Guid.NewGuid();

        context.Users.AddRange(
            User.CreateForGym(myGymId, "mine@a.com", "My", "Coach", "hash", UserRole.Coach),
            User.CreateForGym(otherGymId, "theirs@b.com", "Their", "Coach", "hash", UserRole.Coach));
        context.ClientProfiles.AddRange(
            ClientProfile.Create(myGymId, Guid.NewGuid()),
            ClientProfile.Create(otherGymId, Guid.NewGuid()));
        await context.SaveChangesAsync();

        var result = await new GetDashboardStatsHandler(context, container.CurrentUser)
            .Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        result.TotalCoaches.Should().Be(1);
        result.TotalClients.Should().Be(1);
    }
}
