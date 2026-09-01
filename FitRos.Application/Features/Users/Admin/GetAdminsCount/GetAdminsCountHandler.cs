using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Admin.GetAdminsCount;

public sealed class GetAdminsCountHandler : IRequestHandler<GetAdminsCountQuery, int>
{
    private readonly IFitRosDbContext _context;

    public GetAdminsCountHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public Task<int> Handle(GetAdminsCountQuery request, CancellationToken cancellationToken)
    {
        return _context.Users
            .IgnoreQueryFilters()
            .CountAsync(u => u.Role == UserRole.Admin, cancellationToken);
    }
}
