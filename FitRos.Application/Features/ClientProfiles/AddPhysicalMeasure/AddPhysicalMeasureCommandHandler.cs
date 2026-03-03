using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
 

namespace FitRos.Application.Features.ClientProfiles.AddPhysicalMeasure;

public sealed class AddPhysicalMeasureCommandHandler
    : IRequestHandler<AddPhysicalMeasureCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public AddPhysicalMeasureCommandHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        AddPhysicalMeasureCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var profile = await _context.ClientProfiles
            .Include(x => x.Measures)
            .FirstOrDefaultAsync(
                x => x.Id == request.ClientProfileId,
                cancellationToken);

        if (profile is null)
            throw new NotFoundException("Client profile not found.");

        ValidatePermissions(profile);

        profile.AddMeasure(
            request.Weight,
            request.BodyFatPercentage,
            request.MuscleMass,
            request.Waist,
            request.Chest,
            request.Arms);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException(
                "The client profile was modified by another process. Please reload and try again.");
        }
    }

    private void ValidatePermissions(Domain.Entities.Client.ClientProfile profile)
    {
        if (_currentUser.IsOwner() || _currentUser.IsAdmin())
            return;

        if (_currentUser.IsCoach())
        {
            if (profile.CoachId != _currentUser.UserId)
                throw new ForbiddenException("You do not own this client.");

            return;
        }

        throw new ForbiddenException("You are not authorized.");
    }
}