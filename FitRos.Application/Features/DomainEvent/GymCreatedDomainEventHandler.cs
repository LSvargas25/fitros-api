using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Entities.Auditing;
using FitRos.Domain.Events;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FitRos.Application.Features.DomainEvent
{
    public sealed class GymCreatedDomainEventHandler
      : INotificationHandler<GymCreatedDomainEvent>
    {
        private readonly IFitRosDbContext _context;
        public GymCreatedDomainEventHandler(IFitRosDbContext context) => _context = context;

        public async Task Handle(GymCreatedDomainEvent n, CancellationToken ct)
        {
            _context.AuditLogEntries.Add(AuditLogEntry.Create(
                nameof(GymCreatedDomainEvent),
                JsonSerializer.Serialize(new { n.GymId, n.Name, n.OccurredOn })));
            await _context.SaveChangesAsync(ct);
        }
    }
}
