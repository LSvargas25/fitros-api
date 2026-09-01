using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.ProgressReport.Common;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.ClientProfiles.ProgressReport.GetProgressReport;

public sealed class GetProgressReportHandler
    : IRequestHandler<GetProgressReportQuery, ProgressReportDto>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetProgressReportHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ProgressReportDto> Handle(
        GetProgressReportQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _context.ClientProfiles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.Measures)
            .FirstOrDefaultAsync(x => x.Id == request.ClientProfileId, cancellationToken);

        if (profile is null)
            throw new NotFoundException("Client profile not found.");

        ProgressReportAccess.EnsureCanView(_currentUser, profile);

        var (year, month) = ProgressReportBuilder.ResolvePeriod(request.Year, request.Month);

        return await ProgressReportBuilder.BuildAsync(_context, profile, year, month, cancellationToken);
    }
}
