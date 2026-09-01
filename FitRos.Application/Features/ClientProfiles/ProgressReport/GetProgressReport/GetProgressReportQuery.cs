using FitRos.Application.Features.ClientProfiles.ProgressReport.Common;
using MediatR;

namespace FitRos.Application.Features.ClientProfiles.ProgressReport.GetProgressReport;

/// <summary>Year/Month are optional — omitted means the current UTC month.</summary>
public sealed record GetProgressReportQuery(
    Guid ClientProfileId,
    int? Year,
    int? Month
) : IRequest<ProgressReportDto>;
