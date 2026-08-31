using FitRos.Application.Features.WeeklyTrainingPlans.ActivatePlan;
using FitRos.Application.Features.WeeklyTrainingPlans.AssignRoutineToDay;
using FitRos.Application.Features.WeeklyTrainingPlans.CreatePlan;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.WeeklyTrainingPlans;

// Part 3 — a Coach can build a weekly plan on their own CoachSelf profile using
// the same /api/training-plans handlers, but never on another client's profile.
public class CoachSelfTrainingPlanTests
{
    private static FakeCurrentUser Coach(Guid gymId, Guid coachUserId) => new(gymId)
    {
        UserId = coachUserId,
        Role = UserRole.Coach,
        IsAuthenticated = true
    };

    [Fact]
    public async Task Coach_Can_Create_A_Plan_On_Their_Own_CoachSelf_Profile()
    {
        var gymId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var user = Coach(gymId, coachUserId);
        var ctx = TestDbContextFactory.Create(user);

        var selfProfile = ClientProfile.CreateForCoachSelf(gymId, coachUserId);
        ctx.ClientProfiles.Add(selfProfile);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var result = await new CreateWeeklyTrainingPlanHandler(ctx, user)
            .Handle(new CreateWeeklyTrainingPlanCommand(selfProfile.Id, "My block"), CancellationToken.None);

        var plan = ctx.WeeklyTrainingPlans.Single(p => p.Id == result.Id);
        plan.ClientProfileId.Should().Be(selfProfile.Id);
        plan.CoachId.Should().Be(coachUserId, "a self-plan's coach is the coach themselves");
    }

    [Fact]
    public async Task Coach_Can_Run_The_Full_Cycle_On_Their_Own_Profile()
    {
        var gymId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var user = Coach(gymId, coachUserId);
        var ctx = TestDbContextFactory.Create(user);

        var selfProfile = ClientProfile.CreateForCoachSelf(gymId, coachUserId);
        ctx.ClientProfiles.Add(selfProfile);

        var routine = WorkoutRoutine.Create(gymId, "Push", "x");
        routine.AddExercise(Guid.NewGuid(), 1, 3, 10, 60);
        routine.Publish();
        ctx.WorkoutRoutines.Add(routine);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var created = await new CreateWeeklyTrainingPlanHandler(ctx, user)
            .Handle(new CreateWeeklyTrainingPlanCommand(selfProfile.Id, "My block"), CancellationToken.None);

        await new AssignRoutineToDayHandler(ctx, user)
            .Handle(new AssignRoutineToDayCommand(created.Id, DayOfWeek.Wednesday, routine.Id, null),
                CancellationToken.None);

        await new ActivatePlanHandler(ctx, user)
            .Handle(new ActivatePlanCommand(created.Id), CancellationToken.None);

        var plan = ctx.WeeklyTrainingPlans.Single(p => p.Id == created.Id);
        plan.Status.Should().Be(TrainingPlanStatus.Active);
        plan.Days.Should().ContainSingle().Which.Day.Should().Be(DayOfWeek.Wednesday);
    }

    [Fact]
    public async Task Coach_Cannot_Create_A_Plan_On_Another_Coachs_Self_Profile()
    {
        var gymId = Guid.NewGuid();
        var user = Coach(gymId, Guid.NewGuid());
        var ctx = TestDbContextFactory.Create(user);

        var otherCoachSelf = ClientProfile.CreateForCoachSelf(gymId, Guid.NewGuid());
        ctx.ClientProfiles.Add(otherCoachSelf);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var act = () => new CreateWeeklyTrainingPlanHandler(ctx, user)
            .Handle(new CreateWeeklyTrainingPlanCommand(otherCoachSelf.Id, "nope"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Coach_Cannot_Create_A_Plan_On_A_Real_Client_They_Do_Not_Own()
    {
        var gymId = Guid.NewGuid();
        var user = Coach(gymId, Guid.NewGuid());
        var ctx = TestDbContextFactory.Create(user);

        var someoneElsesClient = ClientProfile.Create(gymId, Guid.NewGuid(), coachId: Guid.NewGuid());
        ctx.ClientProfiles.Add(someoneElsesClient);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var act = () => new CreateWeeklyTrainingPlanHandler(ctx, user)
            .Handle(new CreateWeeklyTrainingPlanCommand(someoneElsesClient.Id, "nope"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
