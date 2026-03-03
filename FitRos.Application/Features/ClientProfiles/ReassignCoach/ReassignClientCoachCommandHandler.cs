using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
 

namespace FitRos.Application.Features.ClientProfiles.ReassignCoach;

public sealed class ReassignClientCoachCommandHandler
    : IRequestHandler<ReassignClientCoachCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ReassignClientCoachCommandHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        ReassignClientCoachCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        if (!_currentUser.IsOwner() && !_currentUser.IsAdmin())
            throw new ForbiddenException("You are not authorized.");

        var client = await _context.ClientProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.Id == request.ClientId,
                cancellationToken);

        if (client is null)
            throw new NotFoundException("Client not found.");

        var newCoach = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.Id == request.NewCoachId,
                cancellationToken);

        if (newCoach is null)
            throw new NotFoundException("Coach not found.");

        if (newCoach.Role != UserRole.Coach)
            throw new DomainException("Assigned user must have Coach role.");

        client.ReassignCoach(request.NewCoachId);

        await _context.SaveChangesAsync(cancellationToken);
    }
}