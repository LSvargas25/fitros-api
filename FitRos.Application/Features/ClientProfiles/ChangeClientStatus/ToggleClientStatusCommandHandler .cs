using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
 

namespace FitRos.Application.Features.ClientProfiles.ChangeClientStatus;

public sealed class ToggleClientStatusCommandHandler
    : IRequestHandler<ToggleClientStatusCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ToggleClientStatusCommandHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        ToggleClientStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var client = await _context.ClientProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.Id == request.ClientId,
                cancellationToken);

        if (client is null)
            throw new NotFoundException("Client not found.");

        if (client.Status == ClientStatus.Deleted)
            throw new DomainException("Deleted clients cannot be modified.");

        ValidatePermissions(client);

        if (request.Activate)
            client.Activate();
        else
            client.Deactivate();

        await _context.SaveChangesAsync(cancellationToken);
    }

    private void ValidatePermissions(Domain.Entities.Client.ClientProfile client)
    {
        if (_currentUser.IsOwner() || _currentUser.IsAdmin())
            return;

        if (_currentUser.IsCoach())
        {
            if (client.CoachId != _currentUser.UserId)
                throw new ForbiddenException("You do not own this client.");

            return;
        }

        throw new ForbiddenException("You are not authorized.");
    }
}