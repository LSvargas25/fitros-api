using FitRos.Application.Features.WeeklyTrainingPlans.AssignRoutineToDay;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.WeeklyTraining;
using FitRos.Domain.Enums;
using FitRos.Tests.Helpers;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.WeeklyTrainingPlans;

// Part 1 — a weekly plan may only reference Published workout routines.
public class AssignRoutinePublishedRuleTests
{
    private static FakeCurrentUser Admin(Guid gymId) => new(gymId)
    {
        UserId = Guid.NewGuid(),
        Role = UserRole.Admin,
        IsAuthenticated = true
    };

    private static WorkoutRoutine PublishedRoutine(Guid gymId)
    {
        var routine = WorkoutRoutine.Create(gymId, $"Routine {Guid.NewGuid()}", "x");
        routine.AddExercise(Guid.NewGuid(), 1, 3, 10, 60);
        routine.Publish();
        return routine;
    }

    private static async Task<(FitRos.Infrastructure.Persistence.FitRosDbContext ctx, FakeCurrentUser user, Guid planId)>
        SeedPlanAsync(Guid gymId)
    {
        var user = Admin(gymId);
        var ctx = TestDbContextFactory.Create(user);

        var client = ClientProfileMother.Create(gymId: gymId, coachId: Guid.NewGuid());
        ctx.ClientProfiles.Add(client);
        var plan = WeeklyTrainingPlan.Create(client.Id, Guid.NewGuid(), gymId, "Week A");
        ctx.WeeklyTrainingPlans.Add(plan);
        await ctx.SaveChangesAsync(CancellationToken.None);

        return (ctx, user, plan.Id);
    }

    [Fact]
    public async Task Assigning_A_Draft_Routine_Is_Rejected()
    {
        var gymId = Guid.NewGuid();
        var (ctx, user, planId) = await SeedPlanAsync(gymId);

        var draft = WorkoutRoutine.Create(gymId, "Draft routine", "x");
        ctx.WorkoutRoutines.Add(draft);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var act = () => new AssignRoutineToDayHandler(ctx, user)
            .Handle(new AssignRoutineToDayCommand(planId, DayOfWeek.Monday, draft.Id, null),
                CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*published*");

        ctx.WeeklyTrainingPlans.Should().ContainSingle()
            .Which.Days.Should().BeEmpty();
    }

    [Fact]
    public async Task Assigning_An_Archived_Routine_Is_Rejected()
    {
        var gymId = Guid.NewGuid();
        var (ctx, user, planId) = await SeedPlanAsync(gymId);

        var archived = PublishedRoutine(gymId);
        archived.Archive();
        ctx.WorkoutRoutines.Add(archived);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var act = () => new AssignRoutineToDayHandler(ctx, user)
            .Handle(new AssignRoutineToDayCommand(planId, DayOfWeek.Monday, archived.Id, null),
                CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Assigning_A_Published_Routine_Succeeds()
    {
        var gymId = Guid.NewGuid();
        var (ctx, user, planId) = await SeedPlanAsync(gymId);

        var published = PublishedRoutine(gymId);
        ctx.WorkoutRoutines.Add(published);
        await ctx.SaveChangesAsync(CancellationToken.None);

        await new AssignRoutineToDayHandler(ctx, user)
            .Handle(new AssignRoutineToDayCommand(planId, DayOfWeek.Monday, published.Id, "leg day"),
                CancellationToken.None);

        var day = ctx.WeeklyTrainingPlans.Single().Days.Should().ContainSingle().Subject;
        day.Day.Should().Be(DayOfWeek.Monday);
        day.WorkoutRoutineId.Should().Be(published.Id);
    }

    [Fact]
    public async Task Unknown_Routine_Still_404s()
    {
        var gymId = Guid.NewGuid();
        var (ctx, user, planId) = await SeedPlanAsync(gymId);

        var act = () => new AssignRoutineToDayHandler(ctx, user)
            .Handle(new AssignRoutineToDayCommand(planId, DayOfWeek.Monday, Guid.NewGuid(), null),
                CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
