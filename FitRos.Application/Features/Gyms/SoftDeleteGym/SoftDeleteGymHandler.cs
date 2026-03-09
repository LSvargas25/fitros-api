using System.Text.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Auditing;
using FitRos.Domain.Entities.Outbox;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Gyms.SoftDeleteGym;

public sealed class SoftDeleteGymHandler : IRequestHandler<SoftDeleteGymCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public SoftDeleteGymHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        SoftDeleteGymCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.OwnerApp)
            throw new ForbiddenException("Only OwnerApp can delete gyms.");

        var gym = await _context.Gyms
            .FirstOrDefaultAsync(x => x.Id == command.GymId, cancellationToken);

        if (gym is null)
            throw new DomainException("Gym not found.");

        gym.SoftDelete();

        var audit = AuditLogEntry.Create(
            "GymDeleted",
            JsonSerializer.Serialize(new
            {
                GymId = gym.Id,
                GymName = gym.Name,
                DeletedBy = _currentUser.UserId
            }));

        _context.AuditLogEntries.Add(audit);

        var outbox = OutboxMessage.Create(
            "GymDeleted",
            JsonSerializer.Serialize(new
            {
                GymId = gym.Id,
                GymName = gym.Name
            }),
            DateTime.UtcNow);

        _context.OutboxMessages.Add(outbox);

        await _context.SaveChangesAsync(cancellationToken);
    }
}