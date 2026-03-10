using System.Text.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Auditing;
using FitRos.Domain.Entities.Outbox;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Gyms.DeactivateGym;

public sealed class DeactivateGymHandler : IRequestHandler<DeactivateGymCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeactivateGymHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        DeactivateGymCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.OwnerApp && _currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("You are not allowed to deactivate gyms.");

        var gym = await _context.Gyms
            .FirstOrDefaultAsync(x => x.Id == command.GymId, cancellationToken);

        if (gym is null)
            throw new DomainException("Gym not found.");

        gym.Deactivate();

        var audit = AuditLogEntry.Create(
            "GymDeactivated",
            JsonSerializer.Serialize(new
            {
                gym.Id,
                DeactivatedBy = _currentUser.UserId
            }));

        _context.AuditLogEntries.Add(audit);

        var outbox = OutboxMessage.Create(
            "GymDeactivated",
            JsonSerializer.Serialize(new
            {
                GymId = gym.Id
            }),
            DateTime.UtcNow);

        _context.OutboxMessages.Add(outbox);

        await _context.SaveChangesAsync(cancellationToken);
    }
}