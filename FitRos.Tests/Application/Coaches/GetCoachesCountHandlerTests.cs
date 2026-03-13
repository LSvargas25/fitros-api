using FitRos.Application.Features.Users.Coach.GetCoachesCount;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Coaches;

public class GetCoachesCountHandlerTests
{
    [Fact]
    public async Task OwnerApp_Gets_Total_Coach_Count_Across_Gyms()
    {
        var gymId1 = Guid.NewGuid();
        var gymId2 = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        context.Users.Add(User.CreateForGym(gymId1, "coach1@test.com", "A", "Coach", "hash", UserRole.Coach));
        context.Users.Add(User.CreateForGym(gymId2, "coach2@test.com", "B", "Coach", "hash", UserRole.Coach));
        context.Users.Add(User.Create("admin@test.com", "C", "Admin", "hash", UserRole.Admin));
        await context.SaveChangesAsync();

        var handler = new GetCoachesCountHandler(context, fakeOwner);
        var result = await handler.Handle(new GetCoachesCountQuery(), CancellationToken.None);

        result.Should().Be(2);
    }

    [Fact]
    public async Task Admin_Gets_Coach_Count_For_Own_Gym_Only()
    {
        var ownGymId = Guid.NewGuid();
        var otherGymId = Guid.NewGuid();

        var fakeAdmin = new FakeCurrentUser(ownGymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeAdmin);
        context.Users.Add(User.CreateForGym(ownGymId, "coach1@test.com", "A", "Coach", "hash", UserRole.Coach));
        context.Users.Add(User.CreateForGym(ownGymId, "coach2@test.com", "B", "Coach", "hash", UserRole.Coach));
        context.Users.Add(User.CreateForGym(otherGymId, "coach3@test.com", "C", "Coach", "hash", UserRole.Coach));
        await context.SaveChangesAsync();

        var handler = new GetCoachesCountHandler(context, fakeAdmin);
        var result = await handler.Handle(new GetCoachesCountQuery(), CancellationToken.None);

        result.Should().Be(2);
    }

    [Fact]
    public async Task Returns_Zero_When_No_Coaches()
    {
        var gymId = Guid.NewGuid();

        var fakeAdmin = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeAdmin);
        var handler = new GetCoachesCountHandler(context, fakeAdmin);

        var result = await handler.Handle(new GetCoachesCountQuery(), CancellationToken.None);

        result.Should().Be(0);
    }
}
