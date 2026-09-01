
using FitRos.Application.Features.DomainEvent;
using FitRos.Domain.Enums;
using FitRos.Domain.Events;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.DomainEvents;

public class GymCreatedDomainEventHandlerTests
{
    [Fact]
    public async Task Should_Create_AuditLogEntry_When_Gym_Created()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var handler = new GymCreatedDomainEventHandler(context);

        var domainEvent = new GymCreatedDomainEvent(Guid.NewGuid(), "Gold's Gym");

        await handler.Handle(domainEvent, CancellationToken.None);

        var audit = context.AuditLogEntries
            .IgnoreQueryFilters()
            .First();

        Assert.Equal(nameof(GymCreatedDomainEvent), audit.EventType);
    }
}