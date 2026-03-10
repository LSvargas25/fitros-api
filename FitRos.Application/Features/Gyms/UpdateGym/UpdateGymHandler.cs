using System.Text.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Auditing;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Outbox;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Gyms.UpdateGym;

public sealed class UpdateGymHandler : IRequestHandler<UpdateGymCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public UpdateGymHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(
        UpdateGymCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.OwnerApp && _currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("You are not allowed to update gyms.");

        var gym = await _context.Gyms
            .FirstOrDefaultAsync(x => x.Id == command.GymId, cancellationToken);

        if (gym is null)
            throw new DomainException("Gym not found.");

        gym.ChangeName(command.Name);
        gym.ChangeAddress(command.Address);
        gym.ChangePhoneNumber(command.PhoneNumber);

        var audit = AuditLogEntry.Create(
            "GymUpdated",
            JsonSerializer.Serialize(new
            {
                gym.Id,
                gym.Name,
                gym.Address,
                gym.PhoneNumber,
                UpdatedBy = _currentUser.UserId
            }));

        _context.AuditLogEntries.Add(audit);

        var outbox = OutboxMessage.Create(
            "GymUpdated",
            JsonSerializer.Serialize(new
            {
                GymId = gym.Id,
                GymName = gym.Name
            }),
            DateTime.UtcNow);

        _context.OutboxMessages.Add(outbox);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    Task IRequestHandler<UpdateGymCommand>.Handle(UpdateGymCommand request, CancellationToken cancellationToken)
    {
        return Handle(request, cancellationToken);
    }
}