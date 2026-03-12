using System.Text.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common;
using FitRos.Domain.Entities.Auditing;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Outbox;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Gyms.CreateGym;

public sealed class CreateGymHandler : IRequestHandler<CreateGymCommand, Guid>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateGymHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateGymCommand command,
        CancellationToken cancellationToken)
    {
        var gym = Gym.Create(
            command.Name,
            command.Address,
            command.PhoneNumber);

        _context.Gyms.Add(gym);

        var audit = AuditLogEntry.Create(
            "GymCreated",
            JsonSerializer.Serialize(new
            {
                gym.Id,
                gym.Name,
                CreatedBy = _currentUser.UserId
            }));

        _context.AuditLogEntries.Add(audit);

        var outbox = OutboxMessage.Create(
            "GymCreated",
            JsonSerializer.Serialize(new
            {
                GymId = gym.Id,
                GymName = gym.Name
            }),
            DateTime.UtcNow);

        _context.OutboxMessages.Add(outbox);

        var ownerAppUsers = await _context.Users
            .Where(u => u.Role == UserRole.OwnerApp)
            .ToListAsync(cancellationToken);

        foreach (var owner in ownerAppUsers)
        {
            var notification = NotificationFactory.GymCreated(owner.Id, gym.Id, gym.Name);
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return gym.Id;
    }
}
