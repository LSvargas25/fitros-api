using FitRos.Application.Features.Users.UpdateUser;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Tests.Application.Users;

public class UpdateUserTests
{
    [Fact]
    public async Task Admin_Should_Change_User_Role()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var user = User.Create(
            "client@test.com",
            "Client",
            "User",
            "hashed",
            UserRole.Client);

        context.Add(user);
        await context.SaveChangesAsync();

        var fakeCurrentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new UpdateUserHandler(context, fakeCurrentUser);

        var command = new UpdateUserCommand(
            user.Id,
            user.FirstName,
            user.LastName,
            null,
            UserRole.Coach);

        await handler.Handle(command, CancellationToken.None);

        var updated = await context.Users
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == user.Id);

        updated.Role.Should().Be(UserRole.Coach);
    }

    [Fact]
    public async Task Coach_Should_Not_Change_User_Role()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var user = User.Create(
            "client@test.com",
            "Client",
            "User",
            "hashed",
            UserRole.Client);

        context.Add(user);
        await context.SaveChangesAsync();

        var fakeCurrentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var handler = new UpdateUserHandler(context, fakeCurrentUser);

        var command = new UpdateUserCommand(
            user.Id,
            user.FirstName,
            user.LastName,
            null,
            UserRole.Coach);

        var act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<ForbiddenException>()
            .WithMessage("You are not authorized.");
    }

    [Fact]
    public async Task Should_Not_Downgrade_Last_Admin()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var admin = User.Create(
            "admin@test.com",
            "Admin",
            "User",
            "hashed",
            UserRole.Admin);

        context.Add(admin);
        await context.SaveChangesAsync();

        var fakeCurrentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new UpdateUserHandler(context, fakeCurrentUser);

        var command = new UpdateUserCommand(
            admin.Id,
            admin.FirstName,
            admin.LastName,
            null,
            UserRole.Coach);

        var act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Cannot downgrade the last Admin.");
    }

    [Fact]
    public async Task Should_Not_Change_Role_If_User_Has_Sessions()
    {
        var gymId = Guid.NewGuid();

        var context = TestDbContextFactory.Create(gymId);

        var client = User.Create(
            "client@test.com",
            "Client",
            "User",
            "hashed",
            UserRole.Client);

        // IMPORTANT
        client.AssignToGym(gymId);

        context.Add(client);
        await context.SaveChangesAsync();

        var session = WorkoutSession.Create(
            gymId,
            client.Id,
            Guid.NewGuid(),
            "Routine",
            1,
            DateTime.UtcNow);

        context.WorkoutSessions.Add(session);
        await context.SaveChangesAsync();

        var fakeCurrentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new UpdateUserHandler(context, fakeCurrentUser);

        var command = new UpdateUserCommand(
            client.Id,
            client.FirstName,
            client.LastName,
            null,
            UserRole.Coach);

        var act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("User with sessions cannot change role.");
    }
}