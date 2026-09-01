using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.ClientProfiles.GetKpiSnapshots;

public sealed class GetClientKpiSnapshotsHandler
    : IRequestHandler<GetClientKpiSnapshotsQuery, List<ClientKpiSnapshotDto>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetClientKpiSnapshotsHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<ClientKpiSnapshotDto>> Handle(
        GetClientKpiSnapshotsQuery request,
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

        // tenant validation (Owner has no gym and may read any client)
        if (!_currentUser.IsOwner() && profile.GymId != _currentUser.GymId)
            throw new ForbiddenException("Client does not belong to your gym.");

        ValidatePermissions(profile);

        // IgnoreQueryFilters: access is already checked above. The global tenant
        // filter is `GymId == currentUser.GymId` with no Owner exception, so
        // without this an Owner (GymId null) would always get an empty list.
        return await _context.ClientKpiSnapshots
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.ClientProfileId == request.ClientProfileId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => new ClientKpiSnapshotDto(
                s.Id,
                s.PhysicalMeasureId,
                s.WeightDelta,
                s.BodyFatDelta,
                s.WaistDelta,
                s.CreatedAtUtc))
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
