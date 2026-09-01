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
    public sealed class WorkoutRoutineCreatedDomainEventHandler
    : INotificationHandler<WorkoutRoutineCreatedDomainEvent>
    {
        private readonly IFitRosDbContext _context;
        public WorkoutRoutineCreatedDomainEventHandler(IFitRosDbContext context) => _context = context;

        public async Task Handle(WorkoutRoutineCreatedDomainEvent n, CancellationToken ct)
        {
            _context.AuditLogEntries.Add(AuditLogEntry.Create(
                nameof(WorkoutRoutineCreatedDomainEvent),
                JsonSerializer.Serialize(new { n.RoutineId, n.UserId, n.OccurredOn })));
            await _context.SaveChangesAsync(ct);
        }
    }
}
