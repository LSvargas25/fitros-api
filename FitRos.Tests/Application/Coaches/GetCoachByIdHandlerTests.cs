using FitRos.Application.Features.Users.Coach.GetCoachById;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Coaches;

public class GetCoachByIdHandlerTests
{
    [Fact]
    public async Task OwnerApp_Gets_Coach_From_Any_Gym()
    {
        var gymId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var coach = User.CreateForGym(gymId, "coach@test.com", "Alice", "Coach", "hash", UserRole.Coach);
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new GetCoachByIdHandler(context, fakeOwner);
        var result = await handler.Handle(new GetCoachByIdQuery(coach.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Email.Should().Be("coach@test.com");
    }

    [Fact]
    public async Task Admin_Gets_Coach_In_Own_Gym()
    {
        var gymId = Guid.NewGuid();

        var fakeAdmin = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeAdmin);
        var coach = User.CreateForGym(gymId, "coach@test.com", "Bob", "Coach", "hash", UserRole.Coach);
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new GetCoachByIdHandler(context, fakeAdmin);
        var result = await handler.Handle(new GetCoachByIdQuery(coach.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(coach.Id);
    }

    [Fact]
    public async Task Admin_Returns_Null_For_Coach_In_Other_Gym()
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
        var coach = User.CreateForGym(otherGymId, "other@test.com", "Carlos", "Coach", "hash", UserRole.Coach);
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new GetCoachByIdHandler(context, fakeAdmin);
        var result = await handler.Handle(new GetCoachByIdQuery(coach.Id), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_Null_When_Id_Not_Found()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var handler = new GetCoachByIdHandler(context, fakeOwner);

        var result = await handler.Handle(new GetCoachByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_Null_When_Id_Belongs_To_Non_Coach_Role()
    {
        var gymId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var admin = User.Create("admin@test.com", "Admin", "User", "hash", UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var handler = new GetCoachByIdHandler(context, fakeOwner);
        var result = await handler.Handle(new GetCoachByIdQuery(admin.Id), CancellationToken.None);

        result.Should().BeNull();
    }
}
