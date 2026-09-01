namespace FitRos.Application.Features.ClientProfiles.ProgressReport.Common;

/// <summary>Current-month value vs previous-month value and their delta.</summary>
public sealed record ProgressMetric(decimal? Current, decimal? Previous, decimal? Delta)
{
    public static ProgressMetric From(decimal? current, decimal? previous)
        => new(current, previous, current.HasValue && previous.HasValue ? current - previous : null);
}

/// <summary>
/// The monthly progress report. Computed on the fly from PhysicalMeasure and
/// completed WorkoutSessions; this same shape is what gets frozen into
/// <c>ClientProgressReportSnapshot.ReportJson</c> when a snapshot is generated.
/// </summary>
public sealed record ProgressReportDto(
    Guid ClientProfileId,
    int Year,
    int Month,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    ProgressMetric Weight,
    ProgressMetric BodyFatPercentage,
    ProgressMetric Waist,
    ProgressMetric CompletedSets,
    DateTime GeneratedAtUtc);
