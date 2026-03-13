using FitRos.Application.Features.Users.Coach.DeactivateCoach;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Coaches;

public class DeactivateCoachHandlerTests
{
    [Fact]
    public async Task Should_Deactivate_Active_Coach()
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
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new DeactivateCoachHandler(context, fakeOwner);
        await handler.Handle(new DeactivateCoachCommand(coach.Id), CancellationToken.None);

        var inDb = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == coach.Id);
        inDb.Status.Should().Be(UserStatus.Inactive);
    }

    [Fact]
    public async Task Admin_Can_Deactivate_Coach_In_Own_Gym()
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
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new DeactivateCoachHandler(context, fakeAdmin);
        await handler.Handle(new DeactivateCoachCommand(coach.Id), CancellationToken.None);

        var inDb = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == coach.Id);
        inDb.Status.Should().Be(UserStatus.Inactive);
    }

    [Fact]
    public async Task Admin_Cannot_Deactivate_Coach_In_Other_Gym()
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
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new DeactivateCoachHandler(context, fakeAdmin);
        var act = async () =>
            await handler.Handle(new DeactivateCoachCommand(coach.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_DomainException_When_Deactivating_Self()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var fakeAdmin = new FakeCurrentUser(gymId)
        {
            UserId = coachId,
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeAdmin);

        var coach = User.CreateForGym(gymId, "self@test.com", "Self", "Coach", "hash", UserRole.Coach);
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(coach, coachId);
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new DeactivateCoachHandler(context, fakeAdmin);
        var act = async () =>
            await handler.Handle(new DeactivateCoachCommand(coachId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("You cannot deactivate yourself.");
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
        var handler = new DeactivateCoachHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new DeactivateCoachCommand(Guid.NewGuid()), CancellationToken.None);

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
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new DeactivateCoachHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new DeactivateCoachCommand(coach.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
