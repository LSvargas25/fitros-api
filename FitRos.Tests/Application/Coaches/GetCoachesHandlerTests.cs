using FitRos.Application.Features.Users.Coach.GetCoaches;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Coaches;

public class GetCoachesHandlerTests
{
    [Fact]
    public async Task OwnerApp_Gets_All_Coaches_Across_Gyms()
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

        var coach1 = User.CreateForGym(gymId1, "coach1@test.com", "Alice", "Coach", "hash", UserRole.Coach);
        var coach2 = User.CreateForGym(gymId2, "coach2@test.com", "Bob", "Coach", "hash", UserRole.Coach);
        context.Users.AddRange(coach1, coach2);
        await context.SaveChangesAsync();

        var handler = new GetCoachesHandler(context, fakeOwner);
        var result = await handler.Handle(new GetCoachesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Admin_Gets_Only_Own_Gym_Coaches()
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

        var coach1 = User.CreateForGym(ownGymId, "coach1@test.com", "Alice", "Coach", "hash", UserRole.Coach);
        var coach2 = User.CreateForGym(otherGymId, "coach2@test.com", "Bob", "Coach", "hash", UserRole.Coach);
        context.Users.AddRange(coach1, coach2);
        await context.SaveChangesAsync();

        var handler = new GetCoachesHandler(context, fakeAdmin);
        var result = await handler.Handle(new GetCoachesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Email.Should().Be("coach1@test.com");
    }

    [Fact]
    public async Task Returns_Empty_List_When_No_Coaches()
    {
        var gymId = Guid.NewGuid();

        var fakeAdmin = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeAdmin);
        var handler = new GetCoachesHandler(context, fakeAdmin);

        var result = await handler.Handle(new GetCoachesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
