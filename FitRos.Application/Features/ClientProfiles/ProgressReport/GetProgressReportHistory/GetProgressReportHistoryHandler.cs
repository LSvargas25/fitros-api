using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.ProgressReport.Common;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.ClientProfiles.ProgressReport.GetProgressReportHistory;

public sealed class GetProgressReportHistoryHandler
    : IRequestHandler<GetProgressReportHistoryQuery, List<ProgressReportSnapshotDto>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetProgressReportHistoryHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<ProgressReportSnapshotDto>> Handle(
        GetProgressReportHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _context.ClientProfiles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientProfileId, cancellationToken);

        if (profile is null)
            throw new NotFoundException("Client profile not found.");

        ProgressReportAccess.EnsureCanView(_currentUser, profile);

        // IgnoreQueryFilters: access already checked above; the tenant filter would
        // hide everything from an Owner (GymId null).
        return await _context.ClientProgressReportSnapshots
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.ClientProfileId == request.ClientProfileId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => new ProgressReportSnapshotDto(
                s.Id,
                s.PhysicalMeasureId,
                s.CreatedAtUtc,
                s.ReportJson))
            .ToListAsync(cancellationToken);
    }
}
