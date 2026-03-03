using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.Users.DeactivateUser;
using MediatR;
using FitRos.Domain.Common;
 
using Microsoft.EntityFrameworkCore;

public sealed class DeactivateUserHandler
    : IRequestHandler<DeactivateUserCommand>
{
    private readonly IFitRosDbContext _context;

    public DeactivateUserHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task Handle(
        DeactivateUserCommand request,
        CancellationToken ct)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user is null)
            throw new NotFoundException("User not found.");

        user.Deactivate();

        await _context.SaveChangesAsync(ct);
    }
}