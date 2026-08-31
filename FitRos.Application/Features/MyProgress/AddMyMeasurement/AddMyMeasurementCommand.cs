using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.AddPhysicalMeasure;
using FitRos.Application.Features.MyPlans.Common;
using MediatR;

namespace FitRos.Application.Features.MyProgress.AddMyMeasurement;

/// <summary>
/// Records a body measurement for the authenticated caller — resolves their
/// ClientProfile from the JWT and delegates to the staff
/// <see cref="AddPhysicalMeasureCommand"/> (which validates + authorizes).
/// </summary>
public sealed record AddMyMeasurementCommand(
    decimal Weight,
    decimal BodyFatPercentage,
    decimal MuscleMass,
    decimal Waist,
    decimal Chest,
    decimal Arms,
    DateTime? RecordedAt = null
) : IRequest;

public sealed class AddMyMeasurementHandler : IRequestHandler<AddMyMeasurementCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ISender _sender;

    public AddMyMeasurementHandler(IFitRosDbContext context, ICurrentUser currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task Handle(AddMyMeasurementCommand request, CancellationToken cancellationToken)
    {
        var profileId = await CallerClientProfile.ResolveIdAsync(_context, _currentUser, cancellationToken);

        await _sender.Send(
            new AddPhysicalMeasureCommand(
                profileId,
                request.Weight,
                request.BodyFatPercentage,
                request.MuscleMass,
                request.Waist,
                request.Chest,
                request.Arms,
                request.RecordedAt),
            cancellationToken);
    }
}
