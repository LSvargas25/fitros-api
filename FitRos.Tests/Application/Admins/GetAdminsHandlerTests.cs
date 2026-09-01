using FitRos.Application.Features.Users.Admin.GetAdmins;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Admins;

public class GetAdminsHandlerTests
{
    [Fact]
    public async Task Returns_All_Admins_With_Gym_Name()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var gym = Gym.Create("CrossFit HQ", "San Jose", "5555-0001");
        context.Gyms.Add(gym);
        await context.SaveChangesAsync(CancellationToken.None);

        var adminWithGym = User.CreateForGym(gym.Id, "admin1@test.com", "With", "Gym", "hash", UserRole.Admin);
        var adminNoGym = User.Create("admin2@test.com", "No", "Gym", "hash", UserRole.Admin);
        context.Users.AddRange(adminWithGym, adminNoGym);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAdminsHandler(context);

        var result = await handler.Handle(new GetAdminsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        var withGymDto = result.First(a => a.GymId.HasValue);
        withGymDto.GymName.Should().Be("CrossFit HQ");

        var noGymDto = result.First(a => !a.GymId.HasValue);
        noGymDto.GymName.Should().BeNull();
    }
}
