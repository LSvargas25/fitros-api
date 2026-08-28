using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.MyPlans.Common;
using FitRos.Application.Features.WeeklyTrainingPlans.GetClientPlans;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MyPlans.GetMyTrainingPlans;

public record GetMyTrainingPlansQuery : IRequest<List<TrainingPlanListItemDto>>;

public sealed class GetMyTrainingPlansHandler
    : IRequestHandler<GetMyTrainingPlansQuery, List<TrainingPlanListItemDto>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMyTrainingPlansHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<TrainingPlanListItemDto>> Handle(
        GetMyTrainingPlansQuery request,
        CancellationToken cancellationToken)
    {
        var profileId = await CallerClientProfile.ResolveIdAsync(_context, _currentUser, cancellationToken);

        return await _context.WeeklyTrainingPlans
            .AsNoTracking()
            .Where(p => p.ClientProfileId == profileId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new TrainingPlanListItemDto(
                p.Id,
                p.Name,
                p.Status,
                p.Days.Count,
                p.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
