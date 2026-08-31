using FitRos.Application.Features.MealPlans.ActivateMealPlan;
using FitRos.Application.Features.MyPlans.GetMyActiveMealPlan;
using FitRos.Application.Features.MyPlans.GetMyActiveTrainingPlan;
using FitRos.Application.Features.WeeklyTrainingPlans.ActivatePlan;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Nutrition;
using FitRos.Domain.Entities.WeeklyTraining;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.MyPlans;

// Part 3/4 — GET /api/my/{meal-plans|training-plans}/active resolves the caller's
// single Active plan (or null). Works for a Coach on their own CoachSelf profile too.
public class MyActivePlanQueryTests
{
    private static FakeCurrentUser Client(Guid gymId, Guid userId) => new(gymId)
    {
        UserId = userId,
        Role = UserRole.Client,
        IsAuthenticated = true
    };

    [Fact]
    public async Task Training_active_returns_null_when_the_caller_has_no_active_plan()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = Client(gymId, userId);
        var ctx = TestDbContextFactory.Create(user);

        var profile = ClientProfile.Create(gymId, userId, Guid.NewGuid());
        ctx.ClientProfiles.Add(profile);
        ctx.WeeklyTrainingPlans.Add(WeeklyTrainingPlan.Create(profile.Id, Guid.NewGuid(), gymId, "Draft only"));
        await ctx.SaveChangesAsync(CancellationToken.None);

        var result = await new GetMyActiveTrainingPlanHandler(ctx, user)
            .Handle(new GetMyActiveTrainingPlanQuery(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Training_active_returns_the_one_active_plan()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = Client(gymId, userId);
        var ctx = TestDbContextFactory.Create(user);

        var profile = ClientProfile.Create(gymId, userId, Guid.NewGuid());
        ctx.ClientProfiles.Add(profile);
        var older = WeeklyTrainingPlan.Create(profile.Id, Guid.NewGuid(), gymId, "Older");
        var newer = WeeklyTrainingPlan.Create(profile.Id, Guid.NewGuid(), gymId, "Newer");
        ctx.WeeklyTrainingPlans.AddRange(older, newer);
        await ctx.SaveChangesAsync(CancellationToken.None);

        // Use the real activate handler so the single-active rule applies.
        var admin = new FakeCurrentUser(gymId) { UserId = Guid.NewGuid(), Role = UserRole.Admin, IsAuthenticated = true };
        var activate = new ActivatePlanHandler(ctx, admin);
        await activate.Handle(new ActivatePlanCommand(older.Id), CancellationToken.None);
        await activate.Handle(new ActivatePlanCommand(newer.Id), CancellationToken.None);

        var result = await new GetMyActiveTrainingPlanHandler(ctx, user)
            .Handle(new GetMyActiveTrainingPlanQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(newer.Id);
    }

    [Fact]
    public async Task Meal_active_returns_null_then_the_active_plan_after_activation()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = Client(gymId, userId);
        var ctx = TestDbContextFactory.Create(user);

        var profile = ClientProfile.Create(gymId, userId, Guid.NewGuid());
        ctx.ClientProfiles.Add(profile);
        var plan = MealPlan.Create(profile.Id, Guid.NewGuid(), gymId, "Cut");
        ctx.MealPlans.Add(plan);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyActiveMealPlanHandler(ctx, user);
        (await handler.Handle(new GetMyActiveMealPlanQuery(), CancellationToken.None)).Should().BeNull();

        var admin = new FakeCurrentUser(gymId) { UserId = Guid.NewGuid(), Role = UserRole.Admin, IsAuthenticated = true };
        await new ActivateMealPlanHandler(ctx, admin)
            .Handle(new ActivateMealPlanCommand(plan.Id), CancellationToken.None);

        var result = await handler.Handle(new GetMyActiveMealPlanQuery(), CancellationToken.None);
        result.Should().NotBeNull();
        result!.Id.Should().Be(plan.Id);
    }

    [Fact]
    public async Task Coach_gets_their_own_CoachSelf_active_training_plan()
    {
        var gymId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var user = new FakeCurrentUser(gymId)
        {
            UserId = coachUserId,
            Role = UserRole.Coach,
            IsAuthenticated = true
        };
        var ctx = TestDbContextFactory.Create(user);

        var selfProfile = ClientProfile.CreateForCoachSelf(gymId, coachUserId);
        ctx.ClientProfiles.Add(selfProfile);
        var plan = WeeklyTrainingPlan.Create(selfProfile.Id, coachUserId, gymId, "My block");
        ctx.WeeklyTrainingPlans.Add(plan);
        await ctx.SaveChangesAsync(CancellationToken.None);

        await new ActivatePlanHandler(ctx, user)
            .Handle(new ActivatePlanCommand(plan.Id), CancellationToken.None);

        var result = await new GetMyActiveTrainingPlanHandler(ctx, user)
            .Handle(new GetMyActiveTrainingPlanQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(plan.Id);
    }
}
