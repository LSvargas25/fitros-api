using FitRos.Application.Features.Users.Client.GetClientById;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Clients;

public class GetClientByIdHandlerTests
{
    [Fact]
    public async Task OwnerApp_Gets_Any_Client()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var gymId = Guid.NewGuid();
        var context = TestDbContextFactory.Create(fakeUser);

        var client = User.CreateForGym(gymId, "client@test.com", "Test", "Client", "hash", UserRole.Client);
        context.Users.Add(client);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClientByIdHandler(context, fakeUser);
        var result = await handler.Handle(new GetClientByIdQuery(client.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Email.Should().Be("client@test.com");
    }

    [Fact]
    public async Task Admin_Cannot_Get_Client_From_Other_Gym()
    {
        var gymId = Guid.NewGuid();
        var fakeAdmin = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeAdmin);

        var otherGymId = Guid.NewGuid();
        var client = User.CreateForGym(otherGymId, "foreign@test.com", "Foreign", "Client", "hash", UserRole.Client);
        context.Users.Add(client);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClientByIdHandler(context, fakeAdmin);

        var act = async () => await handler.Handle(new GetClientByIdQuery(client.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Coach_Cannot_Get_Client_Of_Another_Coach()
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

        var client = User.CreateForGym(gymId, "notmine@test.com", "Not", "Mine", "hash", UserRole.Client);
        context.Users.Add(client);
        await context.SaveChangesAsync(CancellationToken.None);

        var otherCoachId = Guid.NewGuid();
        var profile = ClientProfile.Create(gymId, client.Id, otherCoachId);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClientByIdHandler(context, fakeCoach);

        var act = async () => await handler.Handle(new GetClientByIdQuery(client.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Returns_Null_When_Not_Found()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var handler = new GetClientByIdHandler(context, fakeUser);

        var result = await handler.Handle(new GetClientByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}
