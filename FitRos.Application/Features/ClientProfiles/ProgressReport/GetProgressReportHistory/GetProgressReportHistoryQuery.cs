using MediatR;

namespace FitRos.Application.Features.ClientProfiles.ProgressReport.GetProgressReportHistory;

public sealed record ProgressReportSnapshotDto(
    Guid Id,
    Guid PhysicalMeasureId,
    DateTime CreatedAtUtc,
    string ReportJson);

public sealed record GetProgressReportHistoryQuery(Guid ClientProfileId)
    : IRequest<List<ProgressReportSnapshotDto>>;
