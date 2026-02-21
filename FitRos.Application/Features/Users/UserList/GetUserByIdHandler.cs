using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.GetUserById;

public sealed class GetUserByIdHandler
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetUserByIdHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<UserDetailsDto?> Handle(
        GetUserByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new DomainException("You are not authorized.");

        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == query.Id)
            .Select(u => new UserDetailsDto(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                (int)u.Role,
                (int)u.Status,
                u.CreatedAt,
                u.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}