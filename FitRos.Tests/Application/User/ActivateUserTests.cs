 
using FitRos.Application.Features.Users.ActivateUser;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
 

namespace FitRos.Tests.Application.Users;

public class ActivateUserTests
{
    [Fact]
    public async Task Should_Activate_User_When_User_Is_Inactive()
    {
        var context = TestDbContextFactory.Create();

        var user = User.Create(
            "test@test.com",
            "John",
            "Doe",
            "hashed",
            UserRole.Client);

        user.Deactivate();

        context.Add(user);
        await context.SaveChangesAsync();
        var gymId = Guid.NewGuid();

        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp
        };

        var handler = new ActivateUserHandler(context, currentUser);
        var command = new ActivateUserCommand(user.Id);

        await handler.Handle(command, CancellationToken.None);

        var userInDb = await context.Users
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == user.Id);

        userInDb.Status.Should().Be(UserStatus.Active);
        userInDb.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Not_Throw_When_User_Already_Active()
    {
        var context = TestDbContextFactory.Create();

        var user = User.Create(
            "active@test.com",
            "Jane",
            "Doe",
            "hashed",
            UserRole.Client);

        context.Add(user);
        await context.SaveChangesAsync();

        var gymId = Guid.NewGuid();

        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp
        };

        var handler = new ActivateUserHandler(context, currentUser);
        var command = new ActivateUserCommand(user.Id);

        await handler.Handle(command, CancellationToken.None);

        var userInDb = await context.Users
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == user.Id);

        userInDb.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task Should_Throw_When_User_Not_Found()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp
        };

        var handler = new ActivateUserHandler(context, currentUser);
        var command = new ActivateUserCommand(Guid.NewGuid());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_Forbidden_When_Admin_Tries_To_Activate_Owner()
    {
        var context = TestDbContextFactory.Create();

        var owner = User.Create(
    "owner@test.com",
    "Owner",
    "App",
    "hashed",
    UserRole.Admin); // create as allowed role first

        typeof(User)
            .GetProperty(nameof(User.Role))!
            .SetValue(owner, UserRole.OwnerApp);

        owner.Deactivate();

        context.Add(owner);
        await context.SaveChangesAsync();

        var gymId = Guid.NewGuid();

        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin
        };

        var handler = new ActivateUserHandler(context, currentUser);
        var command = new ActivateUserCommand(owner.Id);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Should_Throw_Unauthorized_When_User_Not_Authenticated()
    {
        var context = TestDbContextFactory.Create();

        var user = User.Create(
            "client@test.com",
            "Client",
            "Test",
            "hashed",
            UserRole.Client);

        context.Add(user);
        await context.SaveChangesAsync();

      

        var gymId = Guid.NewGuid();

        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
         
              IsAuthenticated = false

        };

        var handler = new ActivateUserHandler(context, currentUser);
        var command = new ActivateUserCommand(user.Id);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}