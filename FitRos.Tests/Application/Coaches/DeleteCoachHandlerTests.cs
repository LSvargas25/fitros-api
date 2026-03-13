using FitRos.Application.Features.Users.Coach.DeleteCoach;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Coaches;

public class DeleteCoachHandlerTests
{
    [Fact]
    public async Task Should_Delete_Inactive_Coach()
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

        var handler = new DeleteCoachHandler(context, fakeOwner);
        await handler.Handle(new DeleteCoachCommand(coach.Id), CancellationToken.None);

        var exists = await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == coach.Id);
        exists.Should().BeFalse();
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
        var handler = new DeleteCoachHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new DeleteCoachCommand(Guid.NewGuid()), CancellationToken.None);

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

        var handler = new DeleteCoachHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new DeleteCoachCommand(admin.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_DomainException_When_Coach_Is_Still_Active()
    {
        var gymId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var coach = User.CreateForGym(gymId, "active@test.com", "Active", "Coach", "hash", UserRole.Coach);
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new DeleteCoachHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new DeleteCoachCommand(coach.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Coach must be deactivated before permanent deletion.");
    }

    [Fact]
    public async Task Should_Throw_DomainException_When_Deleting_Self()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = coachId,
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var coach = User.CreateForGym(gymId, "self@test.com", "Self", "Coach", "hash", UserRole.Coach);
        coach.Deactivate();
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(coach, coachId);
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new DeleteCoachHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new DeleteCoachCommand(coachId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("You cannot delete yourself.");
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

        var handler = new DeleteCoachHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new DeleteCoachCommand(coach.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
