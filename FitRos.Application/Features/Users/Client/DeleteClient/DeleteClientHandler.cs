using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Client.DeleteClient;

public sealed class DeleteClientHandler : IRequestHandler<DeleteClientCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeleteClientHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteClientCommand command, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var client = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == command.ClientId && u.Role == UserRole.Client, ct);

        if (client is null)
            throw new NotFoundException("Client not found.");

        if (_currentUser.IsAdmin() && client.GymId != _currentUser.GymId)
            throw new ForbiddenException("Admins can only delete clients within their gym.");

        var profile = await _context.ClientProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(cp => cp.UserId == client.Id, ct);

        if (_currentUser.IsCoach())
        {
            if (profile is null || profile.CoachId != _currentUser.UserId)
                throw new ForbiddenException("Coaches can only delete their own clients.");
        }

        if (client.Status != UserStatus.Inactive)
            throw new DomainException("Client must be deactivated before permanent deletion.");

        _context.Remove(client);

        await _context.SaveChangesAsync(ct);
    }
}
