using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.WeeklyTraining;
using FitRos.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WeeklyTrainingPlans.Common;

/// <summary>
/// Same tenant/coach/self rule every WeeklyTrainingPlans handler needs.
/// Owner: any plan. Admin: any plan in their gym. Coach: only the plan's
/// assigned coach. Client: only their own plan (an independent client has
/// no gym/coach, so the gym check alone isn't enough to stop them acting
/// on someone else's plan - this is what was missing before).
/// </summary>
internal static class WeeklyTrainingPlanAccess
{
    public static void EnsureAuthenticated(ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");
    }

    public static async Task EnsureCanManageAsync(
        ICurrentUser currentUser,
        WeeklyTrainingPlan plan,
        IFitRosDbContext context,
        CancellationToken ct)
    {
        if (currentUser.IsOwner())
            return;

        if (plan.GymId != currentUser.GymId)
            throw new ForbiddenException("Training plan does not belong to your gym.");

        if (currentUser.IsAdmin())
            return;

        if (currentUser.IsCoach())
        {
            if (plan.CoachId != currentUser.UserId)
                throw new ForbiddenException("You are not the assigned coach for this training plan.");
            return;
        }

        if (currentUser.Role == UserRole.Client)
        {
            var ownsProfile = await context.ClientProfiles
                .AnyAsync(cp => cp.Id == plan.ClientProfileId && cp.UserId == currentUser.UserId, ct);

            if (!ownsProfile)
                throw new ForbiddenException("You are not authorized.");
            return;
        }

        throw new ForbiddenException("You are not authorized.");
    }

    public static void EnsureCanAccessClient(ICurrentUser currentUser, ClientProfile clientProfile)
    {
        if (currentUser.IsOwner())
            return;

        if (clientProfile.GymId != currentUser.GymId)
            throw new ForbiddenException("Client does not belong to your gym.");

        if (currentUser.IsAdmin())
            return;

        if (currentUser.IsCoach())
        {
            if (clientProfile.CoachId != currentUser.UserId)
                throw new ForbiddenException("You are not the assigned coach for this client.");
            return;
        }

        if (currentUser.Role == UserRole.Client)
        {
            if (clientProfile.UserId != currentUser.UserId)
                throw new ForbiddenException("You are not authorized.");
            return;
        }

        throw new ForbiddenException("You are not authorized.");
    }
}
