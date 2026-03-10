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
    public sealed class ClientProfileCreatedDomainEventHandler
    : INotificationHandler<ClientProfileCreatedDomainEvent>
    {
        private readonly IFitRosDbContext _context;
        public ClientProfileCreatedDomainEventHandler(IFitRosDbContext context) => _context = context;

        public async Task Handle(ClientProfileCreatedDomainEvent n, CancellationToken ct)
        {
            _context.AuditLogEntries.Add(AuditLogEntry.Create(
                nameof(ClientProfileCreatedDomainEvent),
                JsonSerializer.Serialize(new { n.ClientProfileId, n.GymId, n.OccurredOn })));
            await _context.SaveChangesAsync(ct);
        }
    }
}
