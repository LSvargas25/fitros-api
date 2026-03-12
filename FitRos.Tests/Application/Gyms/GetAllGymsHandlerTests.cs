using FitRos.Application.Features.Gyms.GetAllGyms;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Gyms;

public class GetAllGymsHandlerTests
{
    [Fact]
    public async Task OwnerApp_Gets_All_Gyms_With_Admin_Info()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };
        var context = TestDbContextFactory.Create(fakeUser);

        var gym = Gym.Create("Alpha Gym", "San Jose", "8888-0001");
        context.Gyms.Add(gym);
        await context.SaveChangesAsync(CancellationToken.None);

        var admin = User.CreateForGym(gym.Id, "admin@alpha.com", "Alice", "Admin", "hash", UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllGymsHandler(context);
        var result = await handler.Handle(new GetAllGymsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(gym.Id);
        result[0].Name.Should().Be("Alpha Gym");
        result[0].Admins.Should().HaveCount(1);
        result[0].Admins[0].Id.Should().Be(admin.Id);
        result[0].Admins[0].FullName.Should().Be("Alice Admin");
    }

    [Fact]
    public async Task OwnerApp_Gets_Gym_Without_Admin_When_Not_Assigned()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };
        var context = TestDbContextFactory.Create(fakeUser);

        var gym = Gym.Create("Beta Gym", "Heredia", "8888-0002");
        context.Gyms.Add(gym);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllGymsHandler(context);
        var result = await handler.Handle(new GetAllGymsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Admins.Should().BeEmpty();
    }

    [Fact]
    public async Task Gym_With_Multiple_Admins_Returns_All()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };
        var context = TestDbContextFactory.Create(fakeUser);

        var gym = Gym.Create("Multi Gym", "Alajuela", "8888-0003");
        context.Gyms.Add(gym);
        await context.SaveChangesAsync(CancellationToken.None);

        var admin1 = User.CreateForGym(gym.Id, "admin1@test.com", "Alice", "One", "hash", UserRole.Admin);
        var admin2 = User.CreateForGym(gym.Id, "admin2@test.com", "Bob", "Two", "hash", UserRole.Admin);
        context.Users.AddRange(admin1, admin2);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllGymsHandler(context);
        var result = await handler.Handle(new GetAllGymsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Admins.Should().HaveCount(2);
        result[0].Admins.Should().Contain(a => a.FullName == "Alice One");
        result[0].Admins.Should().Contain(a => a.FullName == "Bob Two");
    }

    [Fact]
    public async Task Deleted_Gyms_Are_Not_Returned()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };
        var context = TestDbContextFactory.Create(fakeUser);

        var activeGym = Gym.Create("Active Gym", "San Jose", "8888-0004");
        var deletedGym = Gym.Create("Deleted Gym", "Cartago", "8888-0005");
        deletedGym.SoftDelete();

        context.Gyms.AddRange(activeGym, deletedGym);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllGymsHandler(context);
        var result = await handler.Handle(new GetAllGymsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Active Gym");
    }
}