using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
 

namespace FitRos.Application.Features.ClientProfiles.SoftDelete;

public sealed class SoftDeleteClientCommandHandler
    : IRequestHandler<SoftDeleteClientCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public SoftDeleteClientCommandHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        SoftDeleteClientCommand request,
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

        ValidatePermissions(client);

        client.SoftDelete();

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