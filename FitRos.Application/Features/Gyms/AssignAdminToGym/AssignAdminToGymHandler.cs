using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Common;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Gyms.AssignAdminToGym;

public sealed class AssignAdminToGymHandler : IRequestHandler<AssignAdminToGymCommand>
{
    private readonly IFitRosDbContext _context;

    public AssignAdminToGymHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task Handle(AssignAdminToGymCommand request, CancellationToken cancellationToken)
    {
        var gym = await _context.Gyms
            .FirstOrDefaultAsync(g => g.Id == request.GymId, cancellationToken);

        if (gym is null)
            throw new NotFoundException($"Gym {request.GymId} not found.");

        var admin = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.AdminId, cancellationToken);

        if (admin is null)
            throw new NotFoundException($"User {request.AdminId} not found.");

        if (admin.Role != UserRole.Admin)
            throw new DomainException("User is not an Admin.");

        if (admin.GymId is not null)
            throw new DomainException("Admin is already assigned to a gym.");

        var adminCount = await _context.Users
            .IgnoreQueryFilters()
            .CountAsync(u => u.GymId == request.GymId
                       && u.Role == UserRole.Admin, cancellationToken);

        if (adminCount >= 3)
            throw new DomainException("A gym cannot have more than 3 administrators.");

        admin.AssignToGym(request.GymId);

        var ownerAppUsers = await _context.Users
            .Where(u => u.Role == UserRole.OwnerApp)
            .ToListAsync(cancellationToken);

        var adminName = $"{admin.FirstName} {admin.LastName}";

        foreach (var owner in ownerAppUsers)
        {
            var notification = NotificationFactory.AdminAssignedToGym(owner.Id, adminName, gym.Name);
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
