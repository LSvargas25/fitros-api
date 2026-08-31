using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.GetPhysicalMeasures;
using FitRos.Application.Features.MyPlans.Common;
using MediatR;

namespace FitRos.Application.Features.MyProgress.GetMyMeasurements;

/// <summary>
/// The authenticated caller's own physical-measure history — resolves their
/// ClientProfile from the JWT (never a caller-supplied id) and delegates to the
/// staff <see cref="GetPhysicalMeasuresQuery"/>. Works for a Client and for a
/// Coach on their own CoachSelf profile.
/// </summary>
public record GetMyMeasurementsQuery : IRequest<List<PhysicalMeasureHistoryDto>>;

public sealed class GetMyMeasurementsHandler
    : IRequestHandler<GetMyMeasurementsQuery, List<PhysicalMeasureHistoryDto>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ISender _sender;

    public GetMyMeasurementsHandler(IFitRosDbContext context, ICurrentUser currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<List<PhysicalMeasureHistoryDto>> Handle(
        GetMyMeasurementsQuery request, CancellationToken cancellationToken)
    {
        var profileId = await CallerClientProfile.ResolveIdAsync(_context, _currentUser, cancellationToken);
        return await _sender.Send(new GetPhysicalMeasuresQuery(profileId), cancellationToken);
    }
}
