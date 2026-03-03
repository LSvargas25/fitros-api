using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using MediatR;
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
        var userQuery = _context.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .ApplyUserVisibility(_currentUser)
            .Where(u => u.Id == query.Id);

        var result = await userQuery
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

        if (result is null)
            throw new NotFoundException("User not found.");

        return result;
    }
}