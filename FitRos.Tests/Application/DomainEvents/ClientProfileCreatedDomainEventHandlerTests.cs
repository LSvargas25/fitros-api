 
using FitRos.Application.Features.DomainEvent;
using FitRos.Domain.Enums;
using FitRos.Domain.Events;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.DomainEvents;

public class ClientProfileCreatedDomainEventHandlerTests
{
    [Fact]
    public async Task Should_Create_AuditLogEntry_When_ClientProfile_Created()
    {
        var fakeUser = new FakeCurrentUser(Guid.NewGuid())
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var handler = new ClientProfileCreatedDomainEventHandler(context);

        var domainEvent = new ClientProfileCreatedDomainEvent(
            Guid.NewGuid(),
            fakeUser.GymId!.Value);

        await handler.Handle(domainEvent, CancellationToken.None);

        var audit = context.AuditLogEntries
            .IgnoreQueryFilters()
            .First();

        Assert.Equal(nameof(ClientProfileCreatedDomainEvent), audit.EventType);
    }
}