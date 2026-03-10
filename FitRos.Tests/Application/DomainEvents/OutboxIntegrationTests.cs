using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Domain.Events;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.DomainEvents;

public class OutboxIntegrationTests
{
    [Fact]
    public async Task Should_Create_OutboxMessage_When_Gym_Created()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.CreateWithInterceptor(fakeUser);  

        var gym = Gym.Create("FitZone", "San José", "88001234");
        context.Gyms.Add(gym);
        await context.SaveChangesAsync();

        var outbox = context.OutboxMessages
            .IgnoreQueryFilters()
            .First();

        Assert.Equal(nameof(GymCreatedDomainEvent), outbox.Type);
        Assert.Null(outbox.ProcessedOnUtc);
    }

    [Fact]
    public async Task Should_Create_OutboxMessage_When_User_Created()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.CreateWithInterceptor(fakeUser);  

        var user = User.Create("john@gym.com", "John", "Doe", "hash123", UserRole.Admin);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var outbox = context.OutboxMessages
            .IgnoreQueryFilters()
            .First();

        Assert.Equal(nameof(UserRegisteredDomainEvent), outbox.Type);
    }
}