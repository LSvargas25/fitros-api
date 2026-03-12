using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Client.GetClientsCount;

public sealed class GetClientsCountHandler : IRequestHandler<GetClientsCountQuery, int>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetClientsCountHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public Task<int> Handle(GetClientsCountQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.Client);

        if (_currentUser.IsAdmin())
            query = query.Where(u => u.GymId == _currentUser.GymId);

        return query.CountAsync(cancellationToken);
    }
}
