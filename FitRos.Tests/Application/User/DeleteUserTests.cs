using FitRos.Application.Features.Users.DeleteUser;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Tests.Application.Users;

public class DeleteUserTests
{
    [Fact]
    public async Task Should_Not_Delete_If_User_Is_Active()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var client = User.Create(
            "client@test.com",
            "Client",
            "User",
            "hashed",
            UserRole.Client);

        context.Add(client);
        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new DeleteUserHandler(context, currentUser);

        var act = async () =>
            await handler.Handle(new DeleteUserCommand(client.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("User must be deactivated before permanent deletion.");
    }

    [Fact]
    public async Task Admin_Should_Delete_Inactive_Client()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var client = User.Create(
            "client@test.com",
            "Client",
            "User",
            "hashed",
            UserRole.Client);

        client.Deactivate();

        context.Add(client);
        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new DeleteUserHandler(context, currentUser);

        await handler.Handle(new DeleteUserCommand(client.Id), CancellationToken.None);

        var exists = await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id == client.Id);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Not_Delete_Last_Admin()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var admin = User.Create(
            "admin@test.com",
            "Admin",
            "User",
            "hashed",
            UserRole.Admin);

        admin.Deactivate();

        context.Add(admin);
        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new DeleteUserHandler(context, currentUser);

        var act = async () =>
            await handler.Handle(new DeleteUserCommand(admin.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Cannot delete the last Admin.");
    }

    [Fact]
    public async Task Should_Not_Delete_Only_Coach()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var coach = User.Create(
            "coach@test.com",
            "Coach",
            "User",
            "hashed",
            UserRole.Coach);

        coach.Deactivate();

        context.Add(coach);
        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new DeleteUserHandler(context, currentUser);

        var act = async () =>
            await handler.Handle(new DeleteUserCommand(coach.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Cannot delete the only Coach.");
    }

    [Fact]
    public async Task Should_Not_Delete_Client_With_Sessions()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var client = User.Create(
            "client@test.com",
            "Client",
            "User",
            "hashed",
            UserRole.Client);

        client.Deactivate();

        context.Add(client);
        await context.SaveChangesAsync();

        var session = WorkoutSession.Create(
            gymId: gymId,
            userId: client.Id,
            routineId: Guid.NewGuid(),
            routineName: "Routine",
            routineVersion: 1,
            scheduledDate: DateTime.UtcNow);

        context.WorkoutSessions.Add(session);
        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new DeleteUserHandler(context, currentUser);

        var act = async () =>
            await handler.Handle(new DeleteUserCommand(client.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Cannot delete user with related workout sessions.");
    }

    [Fact]
    public async Task Coach_Should_Not_Delete_Coach()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var coach = User.Create(
            "coach@test.com",
            "Coach",
            "User",
            "hashed",
            UserRole.Coach);

        coach.Deactivate();

        context.Add(coach);
        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var handler = new DeleteUserHandler(context, currentUser);

        var act = async () =>
            await handler.Handle(new DeleteUserCommand(coach.Id), CancellationToken.None);

        await act.Should()
            .ThrowAsync<ForbiddenException>()
            .WithMessage("Coach can only delete Client users.");
    }

    [Fact]
    public async Task Should_Not_Allow_User_To_Delete_Himself()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var client = User.Create(
            "self@test.com",
            "Self",
            "User",
            "hashed",
            UserRole.Client);

        client.Deactivate();

        context.Add(client);
        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = client.Id,
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new DeleteUserHandler(context, currentUser);

        var act = async () =>
            await handler.Handle(new DeleteUserCommand(client.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("You cannot delete yourself.");
    }
}