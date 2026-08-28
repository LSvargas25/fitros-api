using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.MealPlans.GetClientMealPlans;
using FitRos.Application.Features.MyPlans.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MyPlans.GetMyMealPlans;

public record GetMyMealPlansQuery : IRequest<List<MealPlanListItemDto>>;

public sealed class GetMyMealPlansHandler
    : IRequestHandler<GetMyMealPlansQuery, List<MealPlanListItemDto>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMyMealPlansHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<MealPlanListItemDto>> Handle(
        GetMyMealPlansQuery request,
        CancellationToken cancellationToken)
    {
        var profileId = await CallerClientProfile.ResolveIdAsync(_context, _currentUser, cancellationToken);

        return await _context.MealPlans
            .AsNoTracking()
            .Where(p => p.ClientProfileId == profileId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new MealPlanListItemDto(
                p.Id,
                p.Name,
                p.Status,
                p.Entries.Count,
                p.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
