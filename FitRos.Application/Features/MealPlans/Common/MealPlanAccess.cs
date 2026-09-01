using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Nutrition;

namespace FitRos.Application.Features.MealPlans.Common;

/// <summary>
/// Shared tenant/coach authorization for meal-plan operations, mirroring the
/// inline checks used across the WeeklyTrainingPlans handlers.
/// </summary>
internal static class MealPlanAccess
{
    public static void EnsureAuthenticated(ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");
    }

    public static void EnsureCanManage(ICurrentUser currentUser, MealPlan plan)
    {
        if (plan.GymId != currentUser.GymId && !currentUser.IsOwner())
            throw new ForbiddenException("Meal plan does not belong to your gym.");

        if (currentUser.IsCoach() && plan.CoachId != currentUser.UserId)
            throw new ForbiddenException("You are not the assigned coach for this meal plan.");
    }
}
