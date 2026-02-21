using FitRos.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.DeactivateUser;

public sealed class DeactivateUserHandler : IRequestHandler<DeactivateUserCommand>
{
    private readonly IFitRosDbContext _context;

    public DeactivateUserHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            throw new KeyNotFoundException("User not found.");

        user.Deactivate();

        await _context.SaveChangesAsync(cancellationToken);
    }
}