using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ITenantEntity = FitRos.Domain.Common.ITenantEntity;

namespace FitRos.Application.Features.Gyms.AssignClientToGym;

public sealed class AssignClientToGymHandler : IRequestHandler<AssignClientToGymCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public AssignClientToGymHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(AssignClientToGymCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        if (_currentUser.IsAdmin() && request.GymId != _currentUser.GymId)
            throw new ForbiddenException("Admins can only assign clients to their own gym.");

        var gym = await _context.Gyms
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Id == request.GymId, cancellationToken);

        if (gym is null)
            throw new NotFoundException($"Gym {request.GymId} not found.");

        var client = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.ClientId && u.Role == UserRole.Client, cancellationToken);

        if (client is null)
            throw new NotFoundException($"Client {request.ClientId} not found.");

        if (client.Status != UserStatus.Active)
            throw new DomainException("Client must be active to be assigned to a gym.");

        if (client.GymId.HasValue)
            throw new DomainException("Client is already assigned to a gym.");

        client.AssignToGym(request.GymId);

        var profile = await _context.ClientProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(cp => cp.UserId == client.Id, cancellationToken);

        if (profile is not null)
            ((ITenantEntity)profile).GymId = request.GymId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
