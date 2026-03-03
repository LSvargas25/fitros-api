using FitRos.Application.Features.Users.CreateUser;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Users;

public class CreateUserTests
{


    [Fact]
    public async Task Admin_Should_Create_Coach_Successfully()
    {
        var context = TestDbContextFactory.Create();

        var fakeHasher = new FakePasswordHasher();
        var fakeCurrentUser = new FakeCurrentUser
        {
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new CreateUserHandler(context, fakeHasher, fakeCurrentUser);

        var command = new CreateUserCommand(
            "coach@test.com",
            "John",
            "Doe",
            "Password123");

        var response = await handler.Handle(
            command,
            UserRole.Coach,
            CancellationToken.None);

        response.Should().NotBeNull();
        response.Email.Should().Be("coach@test.com");

        var userInDb = await context.Users.FirstAsync();
        userInDb.PasswordHash.Should().Be("HASHED_Password123");
        userInDb.Role.Should().Be(UserRole.Coach);
    }

    [Fact]
    public async Task Coach_Should_Create_Client_Successfully()
    {
        var context = TestDbContextFactory.Create();

        var fakeHasher = new FakePasswordHasher();
        var fakeCurrentUser = new FakeCurrentUser
        {
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var handler = new CreateUserHandler(context, fakeHasher, fakeCurrentUser);

        var command = new CreateUserCommand(
            "client@test.com",
            "Client",
            "User",
            "Password123");

        var response = await handler.Handle(
            command,
            UserRole.Client,
            CancellationToken.None);

        response.Role.Should().Be((int)UserRole.Client);
    }

    [Fact]
    public async Task Coach_Should_Not_Create_Coach()
    {
        var context = TestDbContextFactory.Create();

        var fakeHasher = new FakePasswordHasher();
        var fakeCurrentUser = new FakeCurrentUser
        {
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var handler = new CreateUserHandler(context, fakeHasher, fakeCurrentUser);

        var command = new CreateUserCommand(
            "coach2@test.com",
            "Another",
            "Coach",
            "Password123");

        var act = async () => await handler.Handle(
            command,
            UserRole.Coach,
            CancellationToken.None);

        await act.Should()
      .ThrowAsync<ForbiddenException>()
      .WithMessage("Coach can only create Client users.");
    }

    [Fact]
    public async Task Should_Not_Create_User_With_Duplicate_Email()
    {
        var context = TestDbContextFactory.Create();

        var fakeHasher = new FakePasswordHasher();
        var fakeCurrentUser = new FakeCurrentUser
        {
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var existingUser = User.Create(
            "duplicate@test.com",
            "Existing",
            "User",
            "hash",
            UserRole.Client);

        context.Users.Add(existingUser);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateUserHandler(context, fakeHasher, fakeCurrentUser);

        var command = new CreateUserCommand(
            "duplicate@test.com",
            "New",
            "User",
            "Password123");

        var act = async () => await handler.Handle(
            command,
            UserRole.Client,
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Email already exists.");
    }

    [Fact]
    public async Task Should_Not_Create_User_If_Not_Authenticated()
    {
        var context = TestDbContextFactory.Create();

        var fakeHasher = new FakePasswordHasher();
        var fakeCurrentUser = new FakeCurrentUser
        {
            Role = UserRole.Admin,
            IsAuthenticated = false
        };

        var handler = new CreateUserHandler(context, fakeHasher, fakeCurrentUser);

        var command = new CreateUserCommand(
            "user@test.com",
            "User",
            "Test",
            "Password123");

        var act = async () => await handler.Handle(
            command,
            UserRole.Client,
            CancellationToken.None);

        await act.Should()
       .ThrowAsync<UnauthorizedException>()
       .WithMessage("User not authenticated.");
    }
}