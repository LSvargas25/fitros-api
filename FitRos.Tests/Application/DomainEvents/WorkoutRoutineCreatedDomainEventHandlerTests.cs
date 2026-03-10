 
using FitRos.Application.Features.DomainEvent;
using FitRos.Domain.Enums;
using FitRos.Domain.Events;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.DomainEvents;

public class WorkoutRoutineCreatedDomainEventHandlerTests
{
    [Fact]
    public async Task Should_Create_AuditLogEntry_When_WorkoutRoutine_Created()
    {
        var fakeUser = new FakeCurrentUser(Guid.NewGuid())
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var handler = new WorkoutRoutineCreatedDomainEventHandler(context);

        var domainEvent = new WorkoutRoutineCreatedDomainEvent(
            Guid.NewGuid(),
            fakeUser.UserId!.Value);

        await handler.Handle(domainEvent, CancellationToken.None);

        var audit = context.AuditLogEntries
            .IgnoreQueryFilters()
            .First();

        Assert.Equal(nameof(WorkoutRoutineCreatedDomainEvent), audit.EventType);
    }
}