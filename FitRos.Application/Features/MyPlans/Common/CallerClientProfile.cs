using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MyPlans.Common;

/// <summary>
/// Resolves the ClientProfile id of the authenticated caller. Used by the
/// "/api/my/*" endpoints so a Client only ever sees their own plans.
/// </summary>
internal static class CallerClientProfile
{
    public static async Task<Guid> ResolveIdAsync(
        IFitRosDbContext context,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new UnauthorizedException("User not authenticated.");

        var profileId = await context.ClientProfiles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(cp => cp.UserId == currentUser.UserId.Value)
            .Select(cp => (Guid?)cp.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return profileId
            ?? throw new NotFoundException("You do not have a client profile.");
    }
}
