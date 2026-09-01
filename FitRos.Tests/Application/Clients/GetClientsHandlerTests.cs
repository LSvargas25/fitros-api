using FitRos.Application.Features.Users.Client.GetClients;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Clients;

public class GetClientsHandlerTests
{
    [Fact]
    public async Task OwnerApp_Gets_All_Clients()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var gymA = Guid.NewGuid();
        var gymB = Guid.NewGuid();
        var context = TestDbContextFactory.Create(fakeUser);

        var client1 = User.CreateForGym(gymA, "c1@test.com", "Client", "One", "hash", UserRole.Client);
        var client2 = User.CreateForGym(gymB, "c2@test.com", "Client", "Two", "hash", UserRole.Client);
        context.Users.AddRange(client1, client2);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClientsHandler(context, fakeUser);
        var result = await handler.Handle(new GetClientsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Admin_Gets_Only_Own_Gym_Clients()
    {
        var gymId = Guid.NewGuid();
        var fakeAdmin = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var otherGymId = Guid.NewGuid();
        var context = TestDbContextFactory.Create(fakeAdmin);

        var clientInGym = User.CreateForGym(gymId, "mine@test.com", "Mine", "Client", "hash", UserRole.Client);
        var clientOther = User.CreateForGym(otherGymId, "other@test.com", "Other", "Client", "hash", UserRole.Client);
        context.Users.AddRange(clientInGym, clientOther);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClientsHandler(context, fakeAdmin);
        var result = await handler.Handle(new GetClientsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Email.Should().Be("mine@test.com");
    }

    [Fact]
    public async Task Coach_Gets_Only_Own_Clients()
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

        var client1 = User.CreateForGym(gymId, "myc@test.com", "My", "Client", "hash", UserRole.Client);
        var client2 = User.CreateForGym(gymId, "notmine@test.com", "Not", "Mine", "hash", UserRole.Client);
        context.Users.AddRange(client1, client2);
        await context.SaveChangesAsync(CancellationToken.None);

        var otherCoachId = Guid.NewGuid();
        var profileMine = ClientProfile.Create(gymId, client1.Id, coachId);
        var profileOther = ClientProfile.Create(gymId, client2.Id, otherCoachId);
        context.ClientProfiles.AddRange(profileMine, profileOther);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClientsHandler(context, fakeCoach);
        var result = await handler.Handle(new GetClientsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Email.Should().Be("myc@test.com");
    }
}
