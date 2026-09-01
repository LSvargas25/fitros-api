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
    public sealed class UserRegisteredDomainEventHandler
     : INotificationHandler<UserRegisteredDomainEvent>
    {
        private readonly IFitRosDbContext _context;
        public UserRegisteredDomainEventHandler(IFitRosDbContext context) => _context = context;

        public async Task Handle(UserRegisteredDomainEvent n, CancellationToken ct)
        {
            _context.AuditLogEntries.Add(AuditLogEntry.Create(
                nameof(UserRegisteredDomainEvent),
                JsonSerializer.Serialize(new { n.UserId, n.OccurredOn })));
            await _context.SaveChangesAsync(ct);
        }
    }
}
 