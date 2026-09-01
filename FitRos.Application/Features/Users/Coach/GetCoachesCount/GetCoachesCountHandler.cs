using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Coach.GetCoachesCount;

public sealed class GetCoachesCountHandler : IRequestHandler<GetCoachesCountQuery, int>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetCoachesCountHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public Task<int> Handle(GetCoachesCountQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.Coach);

        if (_currentUser.IsAdmin())
            query = query.Where(u => u.GymId == _currentUser.GymId);

        return query.CountAsync(cancellationToken);
    }
}
