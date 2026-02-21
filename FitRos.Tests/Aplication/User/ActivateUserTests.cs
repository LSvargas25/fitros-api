using FitRos.Application.Features.Users.ActivateUser;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Tests.Application.Users;

public class ActivateUserTests
{
    private static FitRosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FitRosDbContext(options);
    }

    [Fact]
    public async Task Should_Activate_User_When_User_Is_Inactive()
    {
        // Arrange
        var context = CreateDbContext();

        var user = User.Create(
            "test@test.com",
            "John",
            "Doe",
            "hashed",
            UserRole.Client);

        user.Deactivate();

        context.Add(user);
        await context.SaveChangesAsync();

        var handler = new ActivateUserHandler(context);
        var command = new ActivateUserCommand(user.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var userInDb = await context.Users
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == user.Id);

        userInDb.Status.Should().Be(UserStatus.Active);
        userInDb.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Not_Throw_When_User_Already_Active()
    {
        // Arrange
        var context = CreateDbContext();

        var user = User.Create(
            "active@test.com",
            "Jane",
            "Doe",
            "hashed",
            UserRole.Client);

        context.Add(user);
        await context.SaveChangesAsync();

        var handler = new ActivateUserHandler(context);
        var command = new ActivateUserCommand(user.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var userInDb = await context.Users
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == user.Id);

        userInDb.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task Should_Throw_When_User_Not_Found()
    {
        // Arrange
        var context = CreateDbContext();

        var handler = new ActivateUserHandler(context);
        var command = new ActivateUserCommand(Guid.NewGuid());

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>(); // Reemplaza por NotFoundException si la tienes tipada
    }
}