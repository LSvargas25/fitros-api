
using FitRos.Application.Features.DomainEvent;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Domain.Events;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.DomainEvents;

public class UserRegisteredDomainEventHandlerTests
{
    [Fact]
    public async Task Should_Create_AuditLogEntry_When_User_Registered()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var handler = new UserRegisteredDomainEventHandler(context);

        var domainEvent = new UserRegisteredDomainEvent(Guid.NewGuid(), "test@fitros.com");

        await handler.Handle(domainEvent, CancellationToken.None);

        var audit = context.AuditLogEntries
            .IgnoreQueryFilters()
            .First();

        Assert.Equal(nameof(UserRegisteredDomainEvent), audit.EventType);
    }
}