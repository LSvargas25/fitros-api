using FitRos.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Gyms.GetGymsCount;

public sealed class GetGymsCountHandler : IRequestHandler<GetGymsCountQuery, int>
{
    private readonly IFitRosDbContext _context;

    public GetGymsCountHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public Task<int> Handle(GetGymsCountQuery request, CancellationToken cancellationToken)
        => _context.Gyms
            .Where(g => !g.IsDeleted)
            .CountAsync(cancellationToken);
}
