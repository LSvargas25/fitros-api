using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Foods.ArchiveFood;

public sealed class ArchiveFoodHandler
    : IRequestHandler<ArchiveFoodCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ArchiveFoodHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        ArchiveFoodCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var food = await _context.Foods
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (food is null)
            throw new NotFoundException("Food not found.");

        food.Archive();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
