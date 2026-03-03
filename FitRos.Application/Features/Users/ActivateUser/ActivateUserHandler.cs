using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
 

namespace FitRos.Application.Features.Users.ActivateUser;

public sealed class ActivateUserHandler
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ActivateUserHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(ActivateUserCommand command, CancellationToken ct)
    {
  
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == command.UserId, ct);

        if (user is null)
            throw new NotFoundException("User not found.");

       
        if (!_currentUser.IsOwner())
        {
            if (_currentUser.IsAdmin())
            {
                if (user.Role == UserRole.OwnerApp)
                    throw new ForbiddenException("Admin cannot activate OwnerApp.");
            }
            else
            {
                throw new ForbiddenException("You are not authorized to activate users.");
            }
        }

        user.Activate();

        await _context.SaveChangesAsync(ct);
    }
}