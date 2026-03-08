using System.Text.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Entities.Auditing;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Outbox;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Gyms.CreateGym;

public sealed class CreateGymHandler
{
    private readonly IFitRosDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;

    public CreateGymHandler(
        IFitRosDbContext context,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateGymCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.OwnerApp)
            throw new ForbiddenException("Only OwnerApp can create gyms.");

        var gym = Gym.Create(
            command.Name,
            command.Address,
            command.PhoneNumber);

        _context.Gyms.Add(gym);

        User admin;

        if (command.ExistingAdminUserId.HasValue)
        {
            admin = await _context.Users
                .FirstAsync(x => x.Id == command.ExistingAdminUserId, cancellationToken);

            admin.ChangeRole(UserRole.Admin);
            admin.AssignToGym(gym.Id);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(command.AdminEmail) ||
                string.IsNullOrWhiteSpace(command.AdminPassword))
            {
                throw new DomainException("Admin information is required.");
            }

            var passwordHash = _passwordHasher.Hash(command.AdminPassword);

            admin = User.CreateForGym(
                gym.Id,
                command.AdminEmail,
                command.AdminFirstName!,
                command.AdminLastName!,
                passwordHash,
                UserRole.Admin);

            _context.Users.Add(admin);
        }

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
                GymName = gym.Name,
                AdminUserId = admin.Id
            }),
            DateTime.UtcNow);

        _context.OutboxMessages.Add(outbox);

        await _context.SaveChangesAsync(cancellationToken);

        return gym.Id;
    }
}