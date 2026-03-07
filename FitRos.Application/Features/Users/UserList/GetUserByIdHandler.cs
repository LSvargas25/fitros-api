using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.GetUserById;

public sealed class GetUserByIdHandler
    : IRequestHandler<GetUserByIdQuery, UserDetailsDto>
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

    public async Task<UserDetailsDto> Handle(
      GetUserByIdQuery query,
      CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var user = await _context.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (user is null)
            throw new NotFoundException("User not found.");

        return new UserDetailsDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            (int)user.Role,
            (int)user.Status,
            user.CreatedAt,
            user.UpdatedAt);
    }
}