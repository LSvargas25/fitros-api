using FitRos.Application.Features.Users.Admin.GetAdminById;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Admins;

public class GetAdminByIdHandlerTests
{
    [Fact]
    public async Task Returns_Admin_With_Gym_Info()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var gym = Gym.Create("Detail Gym", "Heredia", "4444-0001");
        context.Gyms.Add(gym);
        await context.SaveChangesAsync(CancellationToken.None);

        var admin = User.CreateForGym(gym.Id, "admin@detail.com", "Detail", "Admin", "hash", UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAdminByIdHandler(context);

        var result = await handler.Handle(new GetAdminByIdQuery(admin.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.GymId.Should().Be(gym.Id);
        result.GymName.Should().Be("Detail Gym");
        result.Email.Should().Be("admin@detail.com");
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
        var handler = new GetAdminByIdHandler(context);

        var result = await handler.Handle(new GetAdminByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}
