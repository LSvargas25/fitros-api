using FitRos.Application.Features.WeeklyTrainingPlans.ActivatePlan;
using FitRos.Application.Features.WeeklyTrainingPlans.ArchivePlan;
using FitRos.Application.Features.WeeklyTrainingPlans.AssignRoutineToDay;
using FitRos.Application.Features.WeeklyTrainingPlans.GetActivePlan;
using FitRos.Application.Features.WeeklyTrainingPlans.GetClientPlans;
using FitRos.Application.Features.WeeklyTrainingPlans.GetPlanById;
using FitRos.Application.Features.WeeklyTrainingPlans.RemoveRoutineFromDay;
using FitRos.Application.Features.WeeklyTrainingPlans.RenamePlan;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.WeeklyTraining;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.WeeklyTrainingPlans;

// BACKEND_AUDIT §25 — every /api/training-plans handler that authorizes a coach
// must let a coach act on their OWN CoachSelf profile / its plans, while still
// 403-ing them on another coach's or a real client's. One test per endpoint,
// both directions.
public class CoachSelfPlanAccessTests
{
    private readonly Guid _gymId = Guid.NewGuid();
    private readonly Guid _coachUserId = Guid.NewGuid();

    private FakeCurrentUser Coach() => new(_gymId)
    {
        UserId = _coachUserId,
        Role = UserRole.Coach,
        IsAuthenticated = true
    };

    private (FitRosDbContext ctx, FakeCurrentUser coach, ClientProfile self, ClientProfile other) Seed()
    {
        var coach = Coach();
        var ctx = TestDbContextFactory.Create(coach);

        var self = ClientProfile.CreateForCoachSelf(_gymId, _coachUserId);          // CoachId == null
        var other = ClientProfile.Create(_gymId, Guid.NewGuid(), Guid.NewGuid());   // a real client, another coach
        ctx.ClientProfiles.AddRange(self, other);
        ctx.SaveChanges();
        return (ctx, coach, self, other);
    }

    private static WorkoutRoutine PublishedRoutine(Guid gymId)
    {
        var r = WorkoutRoutine.Create(gymId, $"R {Guid.NewGuid()}", "x");
        r.AddExercise(Guid.NewGuid(), 1, 3, 10, 60);
        r.Publish();
        return r;
    }

    private Guid PlanFor(FitRosDbContext ctx, ClientProfile profile, Guid? coachId = null)
    {
        var plan = WeeklyTrainingPlan.Create(profile.Id, coachId ?? profile.CoachId ?? _coachUserId, _gymId, "Block");
        ctx.WeeklyTrainingPlans.Add(plan);
        ctx.SaveChanges();
        return plan.Id;
    }

    // ---- keyed by clientProfileId (the reported bug) ----

    [Fact]
    public async Task GetClientPlans_own_self_profile_is_allowed()
    {
        var (ctx, coach, self, _) = Seed();
        PlanFor(ctx, self);

        var act = () => new GetClientPlansHandler(ctx, coach)
            .Handle(new GetClientPlansQuery(self.Id), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetClientPlans_another_clients_profile_is_forbidden()
    {
        var (ctx, coach, _, other) = Seed();

        var act = () => new GetClientPlansHandler(ctx, coach)
            .Handle(new GetClientPlansQuery(other.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetActivePlan_own_self_profile_is_allowed()
    {
        var (ctx, coach, self, _) = Seed();

        var act = () => new GetActiveTrainingPlanHandler(ctx, coach)
            .Handle(new GetActiveTrainingPlanQuery(self.Id), CancellationToken.None);

        await act.Should().NotThrowAsync(); // null result is fine — no active plan
    }

    [Fact]
    public async Task GetActivePlan_another_clients_profile_is_forbidden()
    {
        var (ctx, coach, _, other) = Seed();

        var act = () => new GetActiveTrainingPlanHandler(ctx, coach)
            .Handle(new GetActiveTrainingPlanQuery(other.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ---- keyed by plan id (rely on plan.CoachId stamped at create) ----

    [Fact]
    public async Task GetPlanById_own_self_plan_is_allowed()
    {
        var (ctx, coach, self, _) = Seed();
        var planId = PlanFor(ctx, self);

        var act = () => new GetWeeklyTrainingPlanByIdHandler(ctx, coach)
            .Handle(new GetWeeklyTrainingPlanByIdQuery(planId), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetPlanById_another_coachs_plan_is_forbidden()
    {
        var (ctx, coach, _, other) = Seed();
        var planId = PlanFor(ctx, other, coachId: Guid.NewGuid());

        var act = () => new GetWeeklyTrainingPlanByIdHandler(ctx, coach)
            .Handle(new GetWeeklyTrainingPlanByIdQuery(planId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AssignRoutineToDay_own_self_plan_is_allowed()
    {
        var (ctx, coach, self, _) = Seed();
        var planId = PlanFor(ctx, self);
        var routine = PublishedRoutine(_gymId);
        ctx.WorkoutRoutines.Add(routine);
        ctx.SaveChanges();

        var act = () => new AssignRoutineToDayHandler(ctx, coach)
            .Handle(new AssignRoutineToDayCommand(planId, DayOfWeek.Monday, routine.Id, null), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AssignRoutineToDay_another_coachs_plan_is_forbidden()
    {
        var (ctx, coach, _, other) = Seed();
        var planId = PlanFor(ctx, other, coachId: Guid.NewGuid());
        var routine = PublishedRoutine(_gymId);
        ctx.WorkoutRoutines.Add(routine);
        ctx.SaveChanges();

        var act = () => new AssignRoutineToDayHandler(ctx, coach)
            .Handle(new AssignRoutineToDayCommand(planId, DayOfWeek.Monday, routine.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task RemoveRoutineFromDay_own_self_plan_is_allowed()
    {
        var (ctx, coach, self, _) = Seed();
        var planId = PlanFor(ctx, self);
        var routine = PublishedRoutine(_gymId);
        ctx.WorkoutRoutines.Add(routine);
        ctx.SaveChanges();
        await new AssignRoutineToDayHandler(ctx, coach)
            .Handle(new AssignRoutineToDayCommand(planId, DayOfWeek.Monday, routine.Id, null), CancellationToken.None);

        var act = () => new RemoveRoutineFromDayHandler(ctx, coach)
            .Handle(new RemoveRoutineFromDayCommand(planId, DayOfWeek.Monday), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveRoutineFromDay_another_coachs_plan_is_forbidden()
    {
        var (ctx, coach, _, other) = Seed();
        var planId = PlanFor(ctx, other, coachId: Guid.NewGuid());

        var act = () => new RemoveRoutineFromDayHandler(ctx, coach)
            .Handle(new RemoveRoutineFromDayCommand(planId, DayOfWeek.Monday), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Activate_own_self_plan_is_allowed()
    {
        var (ctx, coach, self, _) = Seed();
        var planId = PlanFor(ctx, self);

        var act = () => new ActivatePlanHandler(ctx, coach)
            .Handle(new ActivatePlanCommand(planId), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Activate_another_coachs_plan_is_forbidden()
    {
        var (ctx, coach, _, other) = Seed();
        var planId = PlanFor(ctx, other, coachId: Guid.NewGuid());

        var act = () => new ActivatePlanHandler(ctx, coach)
            .Handle(new ActivatePlanCommand(planId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Archive_own_self_plan_is_allowed()
    {
        var (ctx, coach, self, _) = Seed();
        var planId = PlanFor(ctx, self);

        var act = () => new ArchivePlanHandler(ctx, coach)
            .Handle(new ArchivePlanCommand(planId), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Archive_another_coachs_plan_is_forbidden()
    {
        var (ctx, coach, _, other) = Seed();
        var planId = PlanFor(ctx, other, coachId: Guid.NewGuid());

        var act = () => new ArchivePlanHandler(ctx, coach)
            .Handle(new ArchivePlanCommand(planId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Rename_own_self_plan_is_allowed()
    {
        var (ctx, coach, self, _) = Seed();
        var planId = PlanFor(ctx, self);

        var act = () => new RenamePlanHandler(ctx, coach)
            .Handle(new RenamePlanCommand(planId, "New name"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Rename_another_coachs_plan_is_forbidden()
    {
        var (ctx, coach, _, other) = Seed();
        var planId = PlanFor(ctx, other, coachId: Guid.NewGuid());

        var act = () => new RenamePlanHandler(ctx, coach)
            .Handle(new RenamePlanCommand(planId, "hijack"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
