using FitRos.Application.Features.Users.Client.GetClientsCount;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Clients;

public class GetClientsCountHandlerTests
{
    [Fact]
    public async Task OwnerApp_Gets_Total_Client_Count_Across_Gyms()
    {
        var gymId1 = Guid.NewGuid();
        var gymId2 = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        context.Users.Add(User.CreateForGym(gymId1, "client1@test.com", "A", "Client", "hash", UserRole.Client));
        context.Users.Add(User.CreateForGym(gymId2, "client2@test.com", "B", "Client", "hash", UserRole.Client));
        context.Users.Add(User.CreateForGym(gymId1, "coach@test.com", "C", "Coach", "hash", UserRole.Coach));
        await context.SaveChangesAsync();

        var handler = new GetClientsCountHandler(context, fakeOwner);
        var result = await handler.Handle(new GetClientsCountQuery(), CancellationToken.None);

        result.Should().Be(2);
    }

    [Fact]
    public async Task Admin_Gets_Client_Count_For_Own_Gym_Only()
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
        context.Users.Add(User.CreateForGym(ownGymId, "client1@test.com", "A", "Client", "hash", UserRole.Client));
        context.Users.Add(User.CreateForGym(ownGymId, "client2@test.com", "B", "Client", "hash", UserRole.Client));
        context.Users.Add(User.CreateForGym(otherGymId, "client3@test.com", "C", "Client", "hash", UserRole.Client));
        await context.SaveChangesAsync();

        var handler = new GetClientsCountHandler(context, fakeAdmin);
        var result = await handler.Handle(new GetClientsCountQuery(), CancellationToken.None);

        result.Should().Be(2);
    }

    [Fact]
    public async Task Returns_Zero_When_No_Clients()
    {
        var gymId = Guid.NewGuid();

        var fakeAdmin = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeAdmin);
        var handler = new GetClientsCountHandler(context, fakeAdmin);

        var result = await handler.Handle(new GetClientsCountQuery(), CancellationToken.None);

        result.Should().Be(0);
    }
}
