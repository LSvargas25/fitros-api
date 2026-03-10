 
using System.Text.Json;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FitRos.Infrastructure.Common.Interceptors;

public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is not null)
            ConvertDomainEventsToOutboxMessages(eventData.Context);

        return base.SavingChangesAsync(eventData, result, ct);
    }

    private static void ConvertDomainEventsToOutboxMessages(DbContext context)
    {
        var outboxMessages = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .SelectMany(aggregate =>
            {
                var events = aggregate.DomainEvents.ToList();
                aggregate.ClearDomainEvents();
                return events;
            })
            .Select(domainEvent => OutboxMessage.Create(
                type: domainEvent.GetType().Name,
                payload: JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                occurredOnUtc: DateTime.UtcNow))
            .ToList();

        context.Set<OutboxMessage>().AddRange(outboxMessages);
    }
}