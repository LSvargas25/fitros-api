 
using FitRos.Domain.Entities.Outbox;
using FitRos.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace FitRos.Infrastructure.Common.BackgroundJobs;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // An exception escaping ExecuteAsync stops the whole host (.NET's
            // default BackgroundServiceExceptionBehavior), so a database that is
            // briefly unreachable - e.g. Neon waking up - would take the API
            // down. Log it and try again on the next tick instead.
            try
            {
                await ProcessOutboxMessagesAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox poll failed; retrying in {Interval}.", Interval);
            }

            await Task.Delay(Interval, ct);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<FitRosDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // Get unprocessed messages
        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null && m.Error == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(20)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                // Resolve the event type from all domain assemblies
                var eventType = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == message.Type);

                if (eventType is null)
                {
                    _logger.LogWarning("Could not resolve type: {Type}", message.Type);
                    message.MarkFailed($"Type not found: {message.Type}");
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType);

                if (domainEvent is not INotification notification)
                {
                    message.MarkFailed("Could not deserialize as INotification");
                    continue;
                }

                await publisher.Publish(notification, ct);
                message.MarkProcessed();

                _logger.LogInformation("Processed outbox message: {Type}", message.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox message: {Type}", message.Type);
                message.MarkFailed(ex.Message);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}