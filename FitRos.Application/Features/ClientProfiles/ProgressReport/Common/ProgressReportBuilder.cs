using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.WorkoutSessions;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.ClientProfiles.ProgressReport.Common;

/// <summary>
/// Builds a <see cref="ProgressReportDto"/> for a given month entirely from
/// existing data — the client's PhysicalMeasure history and their completed
/// WorkoutSessions. Nothing is persisted here.
/// </summary>
internal static class ProgressReportBuilder
{
    public static (int year, int month) ResolvePeriod(int? year, int? month)
    {
        if (year is { } y && month is { } m)
        {
            if (m is < 1 or > 12)
                throw new DomainException("Month must be between 1 and 12.");
            return (y, m);
        }

        var now = DateTime.UtcNow;
        return (now.Year, now.Month);
    }

    public static async Task<ProgressReportDto> BuildAsync(
        IFitRosDbContext context,
        ClientProfile profile,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);
        var previousStart = periodStart.AddMonths(-1);

        // profile.Measures is expected to be loaded by the caller (Include).
        var measures = profile.Measures.OrderByDescending(x => x.RecordedAt).ToList();

        var currentMeasure = measures
            .FirstOrDefault(m => m.RecordedAt >= periodStart && m.RecordedAt < periodEnd);
        var previousMeasure = measures
            .FirstOrDefault(m => m.RecordedAt >= previousStart && m.RecordedAt < periodStart);

        var currentSets = await CountCompletedSetsAsync(
            context, profile.UserId, periodStart, periodEnd, cancellationToken);
        var previousSets = await CountCompletedSetsAsync(
            context, profile.UserId, previousStart, periodStart, cancellationToken);

        return new ProgressReportDto(
            profile.Id,
            year,
            month,
            periodStart,
            periodEnd,
            ProgressMetric.From(currentMeasure?.Weight, previousMeasure?.Weight),
            ProgressMetric.From(currentMeasure?.BodyFatPercentage, previousMeasure?.BodyFatPercentage),
            ProgressMetric.From(currentMeasure?.Waist, previousMeasure?.Waist),
            ProgressMetric.From(currentSets, previousSets),
            DateTime.UtcNow);
    }

    private static async Task<decimal> CountCompletedSetsAsync(
        IFitRosDbContext context,
        Guid userId,
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken cancellationToken)
    {
        // WorkoutSession.Sets is a shadow "_sets" navigation (see
        // WorkoutSessionQueryExtensions), so it can't be counted in SQL. The
        // per-month set is small, so load the completed sessions and count in memory.
        // IgnoreQueryFilters: the caller was already authorized for this exact client
        // by ProgressReportAccess; the tenant filter would otherwise return 0 for an
        // Owner (whose GymId is null).
        var sessions = await context.WorkoutSessions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s =>
                s.UserId == userId &&
                s.Status == WorkoutSessionStatus.Completed &&
                s.ScheduledDate >= fromInclusive &&
                s.ScheduledDate < toExclusive)
            .IncludeSets()
            .ToListAsync(cancellationToken);

        return sessions.Sum(s => s.Sets.Count);
    }
}
