using FitRos.Application.Features.Users.UpdateUser;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Tests.Application.Users;

public class UpdateUserTests
{
    private static FitRosDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new FitRosDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    [Fact]
    public async Task Admin_Should_Update_Email_Successfully()
    {
        var context = CreateDbContext();

        var user = User.Create(
            "old@test.com",
            "Ana",
            "Test",
            "hash",
            UserRole.Client);

        context.Add(user);
        await context.SaveChangesAsync();

        var fakeUser = new FakeCurrentUser
        {
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new UpdateUserHandler(context, fakeUser);

        var command = new UpdateUserCommand(
            user.Id,
            "Ana",
            "Test",
            "new@test.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task Coach_Should_Not_Be_Able_To_Update_Email()
    {
        var context = CreateDbContext();

        var user = User.Create(
            "old@test.com",
            "Ana",
            "Test",
            "hash",
            UserRole.Client);

        context.Add(user);
        await context.SaveChangesAsync();

        var fakeUser = new FakeCurrentUser
        {
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var handler = new UpdateUserHandler(context, fakeUser);

        var command = new UpdateUserCommand(
            user.Id,
            "Ana",
            "Test",
            "new@test.com");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Not_Allow_Duplicate_Email()
    {
        var context = CreateDbContext();

        var user1 = User.Create(
            "one@test.com",
            "Ana",
            "One",
            "hash",
            UserRole.Client);

        var user2 = User.Create(
            "two@test.com",
            "Ana",
            "Two",
            "hash",
            UserRole.Client);

        context.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var fakeUser = new FakeCurrentUser
        {
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new UpdateUserHandler(context, fakeUser);

        var command = new UpdateUserCommand(
            user2.Id,
            "Ana",
            "Two",
            "one@test.com");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Coach_Should_Update_First_And_Last_Name()
    {
        var context = CreateDbContext();

        var user = User.Create(
            "test@test.com",
            "Old",
            "Name",
            "hash",
            UserRole.Client);

        context.Add(user);
        await context.SaveChangesAsync();

        var fakeUser = new FakeCurrentUser
        {
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var handler = new UpdateUserHandler(context, fakeUser);

        var command = new UpdateUserCommand(
            user.Id,
            "New",
            "Name",
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.FirstName.Should().Be("New");
        result.LastName.Should().Be("Name");
    }
}