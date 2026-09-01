using FitRos.Application.Features.Users.Coach.ActivateCoach;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Coaches;

public class ActivateCoachHandlerTests
{
    [Fact]
    public async Task Should_Activate_Inactive_Coach()
    {
        var gymId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var coach = User.CreateForGym(gymId, "coach@test.com", "Alice", "Coach", "hash", UserRole.Coach);
        coach.Deactivate();
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new ActivateCoachHandler(context, fakeOwner);
        await handler.Handle(new ActivateCoachCommand(coach.Id), CancellationToken.None);

        var inDb = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == coach.Id);
        inDb.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task Admin_Can_Activate_Coach_In_Own_Gym()
    {
        var gymId = Guid.NewGuid();

        var fakeAdmin = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeAdmin);
        var coach = User.CreateForGym(gymId, "coach@test.com", "Bob", "Coach", "hash", UserRole.Coach);
        coach.Deactivate();
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new ActivateCoachHandler(context, fakeAdmin);
        await handler.Handle(new ActivateCoachCommand(coach.Id), CancellationToken.None);

        var inDb = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == coach.Id);
        inDb.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task Admin_Cannot_Activate_Coach_In_Other_Gym()
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
        var coach = User.CreateForGym(otherGymId, "other@test.com", "Carlos", "Coach", "hash", UserRole.Coach);
        coach.Deactivate();
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new ActivateCoachHandler(context, fakeAdmin);
        var act = async () =>
            await handler.Handle(new ActivateCoachCommand(coach.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_NotFoundException_When_Coach_Not_Found()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var handler = new ActivateCoachHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new ActivateCoachCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_NotFoundException_When_Id_Belongs_To_Non_Coach_Role()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var admin = User.Create("admin@test.com", "Admin", "User", "hash", UserRole.Admin);
        admin.Deactivate();
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var handler = new ActivateCoachHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new ActivateCoachCommand(admin.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_UnauthorizedException_When_Not_Authenticated()
    {
        var gymId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = false
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var coach = User.CreateForGym(gymId, "coach@test.com", "Alice", "Coach", "hash", UserRole.Coach);
        coach.Deactivate();
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new ActivateCoachHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new ActivateCoachCommand(coach.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
