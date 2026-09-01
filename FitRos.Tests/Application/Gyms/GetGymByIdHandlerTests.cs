using FitRos.Application.Features.Gyms.GetGymById;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Gyms;

public class GetGymByIdHandlerTests
{
    [Fact]
    public async Task OwnerApp_Gets_Any_Gym()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var gym = Gym.Create("My Gym", "Alajuela", "7777-0001");
        context.Gyms.Add(gym);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetGymByIdHandler(context, fakeUser);

        var result = await handler.Handle(new GetGymByIdQuery(gym.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(gym.Id);
        result.Name.Should().Be("My Gym");
    }

    [Fact]
    public async Task Admin_Gets_Own_Gym()
    {
        var gymId = Guid.NewGuid();
        var fakeAdmin = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeAdmin);
        var gym = Gym.Create("Admin Gym", "Cartago", "6666-0001");
        context.Gyms.Add(gym);
        await context.SaveChangesAsync(CancellationToken.None);

        var gymActualId = context.Gyms.First().Id;
        fakeAdmin.GymId = gymActualId;

        var handler = new GetGymByIdHandler(context, fakeAdmin);

        var result = await handler.Handle(new GetGymByIdQuery(gymActualId), CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Admin_Cannot_Get_Other_Gym()
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

        var handler = new GetGymByIdHandler(context, fakeAdmin);

        var act = async () => await handler.Handle(new GetGymByIdQuery(otherGymId), CancellationToken.None);

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
        var handler = new GetGymByIdHandler(context, fakeUser);

        var result = await handler.Handle(new GetGymByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}
