using System.Text.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.ProgressReport.Common;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Reports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.ClientProfiles.ProgressReport.GenerateProgressReport;

public sealed class GenerateProgressReportHandler
    : IRequestHandler<GenerateProgressReportCommand, GenerateProgressReportResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GenerateProgressReportHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<GenerateProgressReportResponse> Handle(
        GenerateProgressReportCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _context.ClientProfiles
            .IgnoreQueryFilters()
            .Include(x => x.Measures)
            .FirstOrDefaultAsync(x => x.Id == request.ClientProfileId, cancellationToken);

        if (profile is null)
            throw new NotFoundException("Client profile not found.");

        ProgressReportAccess.EnsureCanView(_currentUser, profile);

        var (year, month) = ProgressReportBuilder.ResolvePeriod(request.Year, request.Month);

        var report = await ProgressReportBuilder.BuildAsync(
            _context, profile, year, month, cancellationToken);

        // The snapshot table requires a PhysicalMeasureId anchor: prefer the
        // latest measure inside the reported month, else the latest overall.
        var anchorMeasure = profile.Measures
            .Where(m => m.RecordedAt >= report.PeriodStartUtc && m.RecordedAt < report.PeriodEndUtc)
            .OrderByDescending(m => m.RecordedAt)
            .FirstOrDefault()
            ?? profile.Measures
                .OrderByDescending(m => m.RecordedAt)
                .FirstOrDefault()
            ?? throw new DomainException(
                "Client has no physical measures; cannot persist a progress-report snapshot. "
                + "Use GET progress-report for an on-the-fly report instead.");

        var json = JsonSerializer.Serialize(report);

        var snapshot = ClientProgressReportSnapshot.Create(
            profile.GymId ?? Guid.Empty,
            profile.Id,
            anchorMeasure.Id,
            json);

        _context.ClientProgressReportSnapshots.Add(snapshot);

        await _context.SaveChangesAsync(cancellationToken);

        return new GenerateProgressReportResponse(
            snapshot.Id,
            snapshot.PhysicalMeasureId,
            snapshot.CreatedAtUtc,
            report);
    }
}
