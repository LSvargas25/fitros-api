using FitRos.Application.Features.ClientProfiles.ProgressReport.Common;
using MediatR;

namespace FitRos.Application.Features.ClientProfiles.ProgressReport.GenerateProgressReport;

/// <summary>Year/Month optional — omitted means the current UTC month.</summary>
public sealed record GenerateProgressReportCommand(
    Guid ClientProfileId,
    int? Year,
    int? Month
) : IRequest<GenerateProgressReportResponse>;

public sealed record GenerateProgressReportResponse(
    Guid SnapshotId,
    Guid PhysicalMeasureId,
    DateTime CreatedAtUtc,
    ProgressReportDto Report);
