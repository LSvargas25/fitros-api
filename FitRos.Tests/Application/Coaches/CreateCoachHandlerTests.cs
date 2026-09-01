using FitRos.Application.Features.Users.Coach.CreateCoach;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Coaches;

public class CreateCoachHandlerTests
{
    [Fact]
    public async Task OwnerApp_Creates_Coach_Without_Gym()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var hasher = new FakePasswordHasher();
        var handler = new CreateCoachHandler(context, hasher, fakeOwner);

        var command = new CreateCoachCommand("coach@test.com", "John", "Coach", "Password123");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Email.Should().Be("coach@test.com");
        result.Role.Should().Be((int)UserRole.Coach);

        var userInDb = await context.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.Id == result.Id);
        userInDb.GymId.Should().BeNull();
    }

    [Fact]
    public async Task OwnerApp_Creates_Coach_With_GymId()
    {
        var targetGymId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var hasher = new FakePasswordHasher();
        var handler = new CreateCoachHandler(context, hasher, fakeOwner);

        var command = new CreateCoachCommand("coach2@test.com", "Jane", "Coach", "Password123", GymId: targetGymId);

        var result = await handler.Handle(command, CancellationToken.None);

        var userInDb = await context.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.Id == result.Id);
        userInDb.GymId.Should().Be(targetGymId);
        userInDb.Role.Should().Be(UserRole.Coach);
    }

    [Fact]
    public async Task Admin_Creates_Coach_In_Own_Gym()
    {
        var gymId = Guid.NewGuid();

        var fakeAdmin = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeAdmin);
        var hasher = new FakePasswordHasher();
        var handler = new CreateCoachHandler(context, hasher, fakeAdmin);

        var command = new CreateCoachCommand("coach3@test.com", "Bob", "Coach", "Password123");

        var result = await handler.Handle(command, CancellationToken.None);

        var userInDb = await context.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.Id == result.Id);
        userInDb.GymId.Should().Be(gymId);
        userInDb.Role.Should().Be(UserRole.Coach);
    }

    [Fact]
    public async Task Admin_Without_Gym_Throws_DomainException()
    {
        var fakeAdmin = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeAdmin);
        var hasher = new FakePasswordHasher();
        var handler = new CreateCoachHandler(context, hasher, fakeAdmin);

        var command = new CreateCoachCommand("coach4@test.com", "Bad", "Admin", "Password123");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Current user is not assigned to a gym.");
    }

    [Fact]
    public async Task Throws_On_Duplicate_Email()
    {
        var gymId = Guid.NewGuid();

        var fakeAdmin = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeAdmin);

        var existing = User.CreateForGym(gymId, "dup@test.com", "Existing", "User", "hash", UserRole.Coach);
        context.Users.Add(existing);
        await context.SaveChangesAsync(CancellationToken.None);

        var hasher = new FakePasswordHasher();
        var handler = new CreateCoachHandler(context, hasher, fakeAdmin);
        var command = new CreateCoachCommand("dup@test.com", "New", "Coach", "Password123");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Email already exists.");
    }

    [Fact]
    public async Task OwnerApp_Coach_Without_Gym_Only_Notifies_OwnerApp()
    {
        var ownerAppId = Guid.NewGuid();
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = ownerAppId,
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);

        var ownerApp = User.CreateOwnerApp(ownerAppId, "owner@fitros.com", "Owner", "App", "hash");
        context.Users.Add(ownerApp);
        await context.SaveChangesAsync(CancellationToken.None);

        var hasher = new FakePasswordHasher();
        var handler = new CreateCoachHandler(context, hasher, fakeOwner);
        var command = new CreateCoachCommand("coach5@test.com", "No", "Gym", "Password123");

        await handler.Handle(command, CancellationToken.None);

        var notifications = context.Notifications.IgnoreQueryFilters().ToList();
        notifications.Should().Contain(n => n.UserId == ownerAppId);
    }
}
