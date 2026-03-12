using FitRos.Application.Features.Gyms.GetGymsCount;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Gyms;

public class GetGymsCountHandlerTests
{
    [Fact]
    public async Task Returns_Count_Of_Non_Deleted_Gyms()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var gym1 = Gym.Create("Gym A", "City A", "1111-1111");
        var gym2 = Gym.Create("Gym B", "City B", "2222-2222");
        var gym3 = Gym.Create("Gym C", "City C", "3333-3333");
        gym3.SoftDelete();

        context.Gyms.AddRange(gym1, gym2, gym3);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetGymsCountHandler(context);

        var count = await handler.Handle(new GetGymsCountQuery(), CancellationToken.None);

        count.Should().Be(2);
    }
}
