using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.ClientProfiles.ActivateClientInGym;

public sealed class ActivateClientInGymHandler : IRequestHandler<ActivateClientInGymCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ActivateClientInGymHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(ActivateClientInGymCommand request, CancellationToken cancellationToken)
    {
        var profile = await _context.ClientProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(cp => cp.Id == request.ClientProfileId, cancellationToken);

        if (profile is null)
            throw new NotFoundException($"ClientProfile {request.ClientProfileId} not found.");

        // Moving a client between gyms carries their whole history (measures, KPIs,
        // progress reports, plans) across a tenant boundary, so it is OwnerApp-only.
        // An Admin may only (re)activate a client that ALREADY belongs to their gym
        // — which makes this endpoint equivalent to toggle-status for them. There is
        // no "unassigned client" state in the domain (every ClientProfile has a
        // GymId), so this strands nobody. See BACKEND_AUDIT §16.
        if (_currentUser.IsAdmin())
        {
            if (profile.GymId != _currentUser.GymId)
                throw new ForbiddenException(
                    "Admins can only activate a client that already belongs to their gym.");

            if (request.GymId != _currentUser.GymId)
                throw new ForbiddenException("Admins can only activate clients into their own gym.");
        }

        var gym = await _context.Gyms
            .FirstOrDefaultAsync(g => g.Id == request.GymId, cancellationToken);

        if (gym is null)
            throw new NotFoundException($"Gym {request.GymId} not found.");

        ((ITenantEntity)profile).GymId = request.GymId;

        profile.Activate();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
