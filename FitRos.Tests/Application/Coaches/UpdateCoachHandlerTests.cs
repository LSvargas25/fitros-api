using FitRos.Application.Features.Users.Coach.UpdateCoach;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Coaches;

public class UpdateCoachHandlerTests
{
    [Fact]
    public async Task OwnerApp_Can_Update_Coach_In_Any_Gym()
    {
        var gymId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var coach = User.CreateForGym(gymId, "coach@test.com", "Alice", "Old", "hash", UserRole.Coach);
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new UpdateCoachHandler(context, fakeOwner);
        var result = await handler.Handle(
            new UpdateCoachCommand(coach.Id, "Alice", "New", null), CancellationToken.None);

        result.LastName.Should().Be("New");

        var inDb = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == coach.Id);
        inDb.LastName.Should().Be("New");
    }

    [Fact]
    public async Task Admin_Can_Update_Coach_In_Own_Gym()
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

        var handler = new UpdateCoachHandler(context, fakeAdmin);
        var result = await handler.Handle(
            new UpdateCoachCommand(coach.Id, "Robert", null, null), CancellationToken.None);

        result.FirstName.Should().Be("Robert");
    }

    [Fact]
    public async Task Admin_Cannot_Update_Coach_In_Other_Gym()
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

        var handler = new UpdateCoachHandler(context, fakeAdmin);
        var act = async () =>
            await handler.Handle(new UpdateCoachCommand(coach.Id, "Carlos2", null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFoundException_When_Coach_Not_Found()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var handler = new UpdateCoachHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new UpdateCoachCommand(Guid.NewGuid(), "X", "Y", null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_When_Email_Already_In_Use()
    {
        var gymId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var coach1 = User.CreateForGym(gymId, "taken@test.com", "Taken", "Email", "hash", UserRole.Coach);
        var coach2 = User.CreateForGym(gymId, "coach2@test.com", "Coach", "Two", "hash", UserRole.Coach);
        context.Users.AddRange(coach1, coach2);
        await context.SaveChangesAsync();

        var handler = new UpdateCoachHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new UpdateCoachCommand(coach2.Id, null, null, "taken@test.com"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Email already in use.");
    }

    [Fact]
    public async Task Throws_UnauthorizedException_When_Not_Authenticated()
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

        var handler = new UpdateCoachHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new UpdateCoachCommand(coach.Id, "Alice", "Updated", null), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
