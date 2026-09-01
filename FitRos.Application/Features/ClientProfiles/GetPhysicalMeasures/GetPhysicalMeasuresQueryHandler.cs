using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.ClientProfiles.GetPhysicalMeasures;

public sealed class GetPhysicalMeasuresQueryHandler
    : IRequestHandler<GetPhysicalMeasuresQuery, List<PhysicalMeasureHistoryDto>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetPhysicalMeasuresQueryHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<PhysicalMeasureHistoryDto>> Handle(
        GetPhysicalMeasuresQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var profile = await _context.ClientProfiles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.ClientProfileId,
                cancellationToken);

        if (profile is null)
            throw new NotFoundException("Client profile not found.");

        // tenant validation
        if (profile.GymId != _currentUser.GymId)
            throw new ForbiddenException("Client does not belong to your gym.");

        ValidatePermissions(profile);

        return await _context.PhysicalMeasures
            .AsNoTracking()
            .Where(m => m.ClientProfileId == request.ClientProfileId)
            .OrderByDescending(m => m.RecordedAt)
            .Select(m => new PhysicalMeasureHistoryDto(
                m.Id,
                m.Weight,
                m.BodyFatPercentage,
                m.MuscleMass,
                m.Waist,
                m.Chest,
                m.Arms,
                m.RecordedAt))
            .ToListAsync(cancellationToken);
    }

    private void ValidatePermissions(ClientProfile profile)
    {
        if (_currentUser.IsOwner() || _currentUser.IsAdmin())
            return;

        if (_currentUser.IsCoach())
        {
            if (profile.CoachId != _currentUser.UserId)
                throw new ForbiddenException("You do not own this client.");

            return;
        }

        if (Domain.Enums.UserRole.Client == _currentUser.Role)
        {
            if (profile.UserId != _currentUser.UserId)
                throw new ForbiddenException("You are not authorized.");

            return;
        }

        throw new ForbiddenException("You are not authorized.");
    }
}
