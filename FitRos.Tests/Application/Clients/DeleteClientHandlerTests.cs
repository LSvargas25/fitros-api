using FitRos.Application.Features.Users.Client.DeleteClient;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Clients;

public class DeleteClientHandlerTests
{
    [Fact]
    public async Task OwnerApp_Can_Delete_Inactive_Client()
    {
        var gymId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var client = User.CreateForGym(gymId, "client@test.com", "Jane", "Doe", "hash", UserRole.Client);
        client.Deactivate();
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var handler = new DeleteClientHandler(context, fakeOwner);
        await handler.Handle(new DeleteClientCommand(client.Id), CancellationToken.None);

        var exists = await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == client.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Admin_Can_Delete_Client_In_Own_Gym()
    {
        var gymId = Guid.NewGuid();

        var fakeAdmin = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeAdmin);
        var client = User.CreateForGym(gymId, "client@test.com", "Bob", "Client", "hash", UserRole.Client);
        client.Deactivate();
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var handler = new DeleteClientHandler(context, fakeAdmin);
        await handler.Handle(new DeleteClientCommand(client.Id), CancellationToken.None);

        var exists = await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == client.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Admin_Cannot_Delete_Client_In_Other_Gym()
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
        var client = User.CreateForGym(otherGymId, "other@test.com", "Other", "Client", "hash", UserRole.Client);
        client.Deactivate();
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var handler = new DeleteClientHandler(context, fakeAdmin);
        var act = async () =>
            await handler.Handle(new DeleteClientCommand(client.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Coach_Can_Delete_Own_Inactive_Client()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var fakeCoach = new FakeCurrentUser(gymId)
        {
            UserId = coachId,
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeCoach);
        var client = User.CreateForGym(gymId, "client@test.com", "Mark", "Client", "hash", UserRole.Client);
        client.Deactivate();
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var profile = ClientProfile.Create(gymId, client.Id, coachId);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new DeleteClientHandler(context, fakeCoach);
        await handler.Handle(new DeleteClientCommand(client.Id), CancellationToken.None);

        var exists = await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == client.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Coach_Cannot_Delete_Client_Of_Another_Coach()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var otherCoachId = Guid.NewGuid();

        var fakeCoach = new FakeCurrentUser(gymId)
        {
            UserId = coachId,
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeCoach);
        var client = User.CreateForGym(gymId, "client@test.com", "Mark", "Client", "hash", UserRole.Client);
        client.Deactivate();
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var profile = ClientProfile.Create(gymId, client.Id, otherCoachId);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new DeleteClientHandler(context, fakeCoach);
        var act = async () =>
            await handler.Handle(new DeleteClientCommand(client.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Throws_DomainException_When_Client_Is_Still_Active()
    {
        var gymId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var client = User.CreateForGym(gymId, "client@test.com", "Active", "Client", "hash", UserRole.Client);
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var handler = new DeleteClientHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new DeleteClientCommand(client.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Client must be deactivated before permanent deletion.");
    }

    [Fact]
    public async Task Throws_NotFoundException_When_Client_Not_Found()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var handler = new DeleteClientHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new DeleteClientCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
