using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
 

namespace FitRos.Application.Features.ClientProfiles.HardDelete;

public sealed class HardDeleteClientCommandHandler
    : IRequestHandler<HardDeleteClientCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public HardDeleteClientCommandHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        HardDeleteClientCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        if (!_currentUser.IsOwner() && !_currentUser.IsAdmin())
            throw new ForbiddenException("You are not authorized.");

        var client = await _context.ClientProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.Id == request.ClientId,
                cancellationToken);

        if (client is null)
            throw new NotFoundException("Client not found.");

        if (client.Status != ClientStatus.Deleted)
            throw new DomainException("Client must be soft deleted before hard delete.");

        _context.ClientProfiles.Remove(client);

        await _context.SaveChangesAsync(cancellationToken);
    }
}