using FitRos.Application.Features.Users.Client.UpdateClient;
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

public class UpdateClientHandlerTests
{
    [Fact]
    public async Task OwnerApp_Can_Update_Any_Client()
    {
        var gymId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var client = User.CreateForGym(gymId, "client@test.com", "Jane", "Old", "hash", UserRole.Client);
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var handler = new UpdateClientHandler(context, fakeOwner);
        var result = await handler.Handle(
            new UpdateClientCommand(client.Id, "Jane", "New", null), CancellationToken.None);

        result.LastName.Should().Be("New");

        var inDb = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == client.Id);
        inDb.LastName.Should().Be("New");
    }

    [Fact]
    public async Task Admin_Can_Update_Client_In_Own_Gym()
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
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var handler = new UpdateClientHandler(context, fakeAdmin);
        var result = await handler.Handle(
            new UpdateClientCommand(client.Id, "Robert", null, null), CancellationToken.None);

        result.FirstName.Should().Be("Robert");
    }

    [Fact]
    public async Task Admin_Cannot_Update_Client_In_Other_Gym()
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
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var handler = new UpdateClientHandler(context, fakeAdmin);
        var act = async () =>
            await handler.Handle(new UpdateClientCommand(client.Id, "X", "Y", null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Coach_Can_Update_Own_Client()
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
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var profile = ClientProfile.Create(gymId, client.Id, coachId);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new UpdateClientHandler(context, fakeCoach);
        var result = await handler.Handle(
            new UpdateClientCommand(client.Id, "Marcus", null, null), CancellationToken.None);

        result.FirstName.Should().Be("Marcus");
    }

    [Fact]
    public async Task Coach_Cannot_Update_Client_Of_Another_Coach()
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
        context.Users.Add(client);
        await context.SaveChangesAsync();

        var profile = ClientProfile.Create(gymId, client.Id, otherCoachId);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new UpdateClientHandler(context, fakeCoach);
        var act = async () =>
            await handler.Handle(new UpdateClientCommand(client.Id, "X", "Y", null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
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
        var handler = new UpdateClientHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new UpdateClientCommand(Guid.NewGuid(), "X", "Y", null), CancellationToken.None);

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
        var client1 = User.CreateForGym(gymId, "taken@test.com", "Taken", "Email", "hash", UserRole.Client);
        var client2 = User.CreateForGym(gymId, "client2@test.com", "Client", "Two", "hash", UserRole.Client);
        context.Users.AddRange(client1, client2);
        await context.SaveChangesAsync();

        var handler = new UpdateClientHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new UpdateClientCommand(client2.Id, null, null, "taken@test.com"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Email already in use.");
    }
}
