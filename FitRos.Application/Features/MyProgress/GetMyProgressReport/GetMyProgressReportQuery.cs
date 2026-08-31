using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.ProgressReport.Common;
using FitRos.Application.Features.ClientProfiles.ProgressReport.GetProgressReport;
using FitRos.Application.Features.MyPlans.Common;
using MediatR;

namespace FitRos.Application.Features.MyProgress.GetMyProgressReport;

/// <summary>
/// The authenticated caller's own month-vs-month progress report. Resolves the
/// caller's ClientProfile from the JWT and delegates to
/// <see cref="GetProgressReportQuery"/>. Year/Month optional (current UTC month).
/// </summary>
public sealed record GetMyProgressReportQuery(int? Year, int? Month) : IRequest<ProgressReportDto>;

public sealed class GetMyProgressReportHandler
    : IRequestHandler<GetMyProgressReportQuery, ProgressReportDto>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ISender _sender;

    public GetMyProgressReportHandler(IFitRosDbContext context, ICurrentUser currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<ProgressReportDto> Handle(
        GetMyProgressReportQuery request, CancellationToken cancellationToken)
    {
        var profileId = await CallerClientProfile.ResolveIdAsync(_context, _currentUser, cancellationToken);
        return await _sender.Send(
            new GetProgressReportQuery(profileId, request.Year, request.Month), cancellationToken);
    }
}
