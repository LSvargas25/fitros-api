using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Analytics;
using FitRos.Domain.Entities.Client;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.ClientProfiles.GenerateKpiSnapshot;

public sealed class GenerateClientKpiSnapshotCommandHandler
    : IRequestHandler<GenerateClientKpiSnapshotCommand, GenerateClientKpiSnapshotResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GenerateClientKpiSnapshotCommandHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<GenerateClientKpiSnapshotResponse> Handle(
        GenerateClientKpiSnapshotCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var profile = await _context.ClientProfiles
            .IgnoreQueryFilters()
            .Include(x => x.Measures)
            .FirstOrDefaultAsync(
                x => x.Id == request.ClientProfileId,
                cancellationToken);

        if (profile is null)
            throw new NotFoundException("Client profile not found.");

        // tenant validation
        if (profile.GymId != _currentUser.GymId)
            throw new ForbiddenException("Client does not belong to your gym.");

        ValidatePermissions(profile);

        var orderedMeasures = profile.Measures
            .OrderByDescending(m => m.RecordedAt)
            .ToList();

        var latest = orderedMeasures.FirstOrDefault()
            ?? throw new DomainException(
                "Client has no physical measures to generate a KPI snapshot from.");

        var previous = orderedMeasures.Skip(1).FirstOrDefault();

        var snapshot = ClientKpiSnapshot.Create(
            profile.Id,
            latest.Id,
            previous is null ? null : latest.Weight - previous.Weight,
            previous is null ? null : latest.BodyFatPercentage - previous.BodyFatPercentage,
            previous is null ? null : latest.Waist - previous.Waist);

        _context.ClientKpiSnapshots.Add(snapshot);

        await _context.SaveChangesAsync(cancellationToken);

        return new GenerateClientKpiSnapshotResponse(
            snapshot.Id,
            snapshot.PhysicalMeasureId,
            snapshot.WeightDelta,
            snapshot.BodyFatDelta,
            snapshot.WaistDelta);
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
