using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Gyms.AssignCoachToGym;

public sealed class AssignCoachToGymHandler : IRequestHandler<AssignCoachToGymCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public AssignCoachToGymHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(AssignCoachToGymCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var gym = await _context.Gyms
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Id == request.GymId, cancellationToken);

        if (gym is null)
            throw new NotFoundException($"Gym {request.GymId} not found.");

        var coach = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.CoachId && u.Role == UserRole.Coach, cancellationToken);

        if (coach is null)
            throw new NotFoundException($"Coach {request.CoachId} not found.");

        if (coach.Status != UserStatus.Active)
            throw new DomainException("Coach must be active to be assigned to a gym.");

        if (coach.GymId.HasValue && coach.GymId != request.GymId)
            throw new DomainException("Coach is already assigned to a different gym.");

        coach.AssignToGym(request.GymId);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
