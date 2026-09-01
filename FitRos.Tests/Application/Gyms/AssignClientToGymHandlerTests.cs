using FitRos.Application.Features.Gyms.AssignClientToGym;
using FitRos.Application.Features.Gyms.CreateGym;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Gyms;

public class AssignClientToGymHandlerTests
{
    private static async Task<Guid> CreateGymAsync(
        FitRos.Infrastructure.Persistence.FitRosDbContext context,
        FakeCurrentUser fakeOwner)
    {
        var gymHandler = new CreateGymHandler(context, fakeOwner);
        return await gymHandler.Handle(
            new CreateGymCommand("Test Gym", "Test City", "5551234"),
            CancellationToken.None);
    }

    [Fact]
    public async Task OwnerApp_Should_Assign_Client_To_Gym()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var gymId = await CreateGymAsync(context, fakeOwner);

        var client = User.Create("client@test.com", "Jane", "Client", "hash", UserRole.Client);
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var handler = new AssignClientToGymHandler(context, fakeOwner);
        await handler.Handle(new AssignClientToGymCommand(gymId, client.Id), CancellationToken.None);

        var inDb = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == client.Id);
        inDb.GymId.Should().Be(gymId);
    }

    [Fact]
    public async Task Admin_Should_Assign_Client_To_Own_Gym()
    {
        var gym = Gym.Create("Admin Gym", "City", "1234");
        var gymId = gym.Id;

        // Use OwnerApp context to seed entities so SaveChanges doesn't auto-assign GymId to the client
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };
        var context = TestDbContextFactory.Create(fakeOwner);
        context.Gyms.Add(gym);

        var client = User.Create("client@test.com", "Bob", "Client", "hash", UserRole.Client);
        context.Users.Add(client);
        await context.SaveChangesAsync();

        // Admin is the authorized actor for the operation
        var fakeAdmin = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new AssignClientToGymHandler(context, fakeAdmin);
        await handler.Handle(new AssignClientToGymCommand(gymId, client.Id), CancellationToken.None);

        var inDb = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == client.Id);
        inDb.GymId.Should().Be(gymId);
    }

    [Fact]
    public async Task Admin_Cannot_Assign_Client_To_Different_Gym()
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
        var client = User.Create("client@test.com", "Bob", "Client", "hash", UserRole.Client);
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var handler = new AssignClientToGymHandler(context, fakeAdmin);
        var act = async () =>
            await handler.Handle(new AssignClientToGymCommand(otherGymId, client.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Should_Throw_NotFoundException_When_Gym_Not_Found()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var client = User.Create("client@test.com", "Jane", "Client", "hash", UserRole.Client);
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var handler = new AssignClientToGymHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new AssignClientToGymCommand(Guid.NewGuid(), client.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_NotFoundException_When_Client_Not_Found()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var gymId = await CreateGymAsync(context, fakeOwner);

        var handler = new AssignClientToGymHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new AssignClientToGymCommand(gymId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_DomainException_When_Client_Is_Inactive()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var gymId = await CreateGymAsync(context, fakeOwner);

        var client = User.Create("client@test.com", "Jane", "Client", "hash", UserRole.Client);
        client.Deactivate();
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var handler = new AssignClientToGymHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new AssignClientToGymCommand(gymId, client.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Client must be active to be assigned to a gym.");
    }

    [Fact]
    public async Task Should_Throw_DomainException_When_Client_Already_Has_Gym()
    {
        var existingGymId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var gymId = await CreateGymAsync(context, fakeOwner);

        var client = User.CreateForGym(existingGymId, "client@test.com", "Jane", "Client", "hash", UserRole.Client);
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var handler = new AssignClientToGymHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new AssignClientToGymCommand(gymId, client.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Client is already assigned to a gym.");
    }
}
