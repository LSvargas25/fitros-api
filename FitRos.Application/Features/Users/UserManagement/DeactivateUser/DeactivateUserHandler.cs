using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.UserManagement.DeactivateUser;

public sealed class DeactivateUserHandler
    : IRequestHandler<DeactivateUserCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeactivateUserHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        DeactivateUserCommand request,
        CancellationToken ct)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.Id == request.UserId &&
                     x.GymId == _currentUser.GymId,
                ct);

        if (user is null)
            throw new NotFoundException("User not found.");

        user.Deactivate();

        await _context.SaveChangesAsync(ct);
    }
}