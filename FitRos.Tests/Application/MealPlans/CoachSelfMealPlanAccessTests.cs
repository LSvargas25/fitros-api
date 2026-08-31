using FitRos.Application.Features.MealPlans.GetActiveMealPlan;
using FitRos.Application.Features.MealPlans.GetClientMealPlans;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.MealPlans;

// BACKEND_AUDIT §25 — the meal-plan client-scoped READ handlers get the same
// CoachSelf exception as the training-plan ones, so a coach isn't 403'd looking
// at their own area. (Meal-plan self-authoring stays out of scope — §22.2.)
public class CoachSelfMealPlanAccessTests
{
    private readonly Guid _gymId = Guid.NewGuid();
    private readonly Guid _coachUserId = Guid.NewGuid();

    private FakeCurrentUser Coach() => new(_gymId)
    {
        UserId = _coachUserId,
        Role = UserRole.Coach,
        IsAuthenticated = true
    };

    private (FitRos.Infrastructure.Persistence.FitRosDbContext ctx, FakeCurrentUser coach,
             ClientProfile self, ClientProfile other) Seed()
    {
        var coach = Coach();
        var ctx = TestDbContextFactory.Create(coach);
        var self = ClientProfile.CreateForCoachSelf(_gymId, _coachUserId);
        var other = ClientProfile.Create(_gymId, Guid.NewGuid(), Guid.NewGuid());
        ctx.ClientProfiles.AddRange(self, other);
        ctx.SaveChanges();
        return (ctx, coach, self, other);
    }

    [Fact]
    public async Task GetClientMealPlans_own_self_profile_is_allowed()
    {
        var (ctx, coach, self, _) = Seed();

        var act = () => new GetClientMealPlansHandler(ctx, coach)
            .Handle(new GetClientMealPlansQuery(self.Id), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetClientMealPlans_another_clients_profile_is_forbidden()
    {
        var (ctx, coach, _, other) = Seed();

        var act = () => new GetClientMealPlansHandler(ctx, coach)
            .Handle(new GetClientMealPlansQuery(other.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetActiveMealPlan_own_self_profile_is_allowed()
    {
        var (ctx, coach, self, _) = Seed();

        var act = () => new GetActiveMealPlanHandler(ctx, coach)
            .Handle(new GetActiveMealPlanQuery(self.Id), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetActiveMealPlan_another_clients_profile_is_forbidden()
    {
        var (ctx, coach, _, other) = Seed();

        var act = () => new GetActiveMealPlanHandler(ctx, coach)
            .Handle(new GetActiveMealPlanQuery(other.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
