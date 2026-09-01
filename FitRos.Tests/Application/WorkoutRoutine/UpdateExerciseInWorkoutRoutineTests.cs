using FitRos.Application.Features.WorkoutRoutines.UpdateExerciseInWorkoutRoutine;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class UpdateExerciseInWorkoutRoutineTests
{
    private static (FitRos.Infrastructure.Persistence.FitRosDbContext ctx, WorkoutRoutine routine, Exercise exercise, Guid gymId)
        SeedDraftRoutineWithExercise()
    {
        var container = TestDbContextFactory.CreateContainer();
        var context = container.Context;
        var gymId = container.CurrentUser.GymId!.Value;

        var routine = WorkoutRoutine.Create(gymId, "Test Routine", "Desc");
        var exercise = Exercise.Create("Bench Press", "Chest", MuscleGroup.Chest, gymId);
        routine.AddExercise(exercise.Id, 1, 3, 10, 90);

        context.WorkoutRoutines.Add(routine);
        context.Exercises.Add(exercise);
        context.SaveChanges();

        return (context, routine, exercise, gymId);
    }

    [Fact]
    public async Task Handle_Updates_Targets_In_A_Draft_Routine()
    {
        var (context, routine, exercise, _) = SeedDraftRoutineWithExercise();
        var handler = new UpdateExerciseInWorkoutRoutineHandler(context);

        var result = await handler.Handle(
            new UpdateExerciseInWorkoutRoutineCommand(routine.Id, exercise.Id, 4, 8, 120),
            CancellationToken.None);

        result.Should().BeTrue();

        var re = await context.WorkoutRoutines
            .IgnoreQueryFilters()
            .Include(r => r.Exercises)
            .FirstAsync(r => r.Id == routine.Id);

        var updated = re.Exercises.Single(e => e.ExerciseId == exercise.Id);
        updated.SuggestedSets.Should().Be(4);
        updated.SuggestedReps.Should().Be(8);
        updated.SuggestedRestSeconds.Should().Be(120);
        updated.Order.Should().Be(1); // position preserved
    }

    [Fact]
    public async Task Handle_Throws_When_Routine_Not_Found()
    {
        var (context, _, _, _) = SeedDraftRoutineWithExercise();
        var handler = new UpdateExerciseInWorkoutRoutineHandler(context);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                new UpdateExerciseInWorkoutRoutineCommand(Guid.NewGuid(), Guid.NewGuid(), 4, 8, 120),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Throws_When_Exercise_Not_In_Routine()
    {
        var (context, routine, _, _) = SeedDraftRoutineWithExercise();
        var handler = new UpdateExerciseInWorkoutRoutineHandler(context);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                new UpdateExerciseInWorkoutRoutineCommand(routine.Id, Guid.NewGuid(), 4, 8, 120),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Throws_When_Routine_Is_Published()
    {
        var (context, routine, exercise, _) = SeedDraftRoutineWithExercise();

        routine.Publish();
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateExerciseInWorkoutRoutineHandler(context);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                new UpdateExerciseInWorkoutRoutineCommand(routine.Id, exercise.Id, 4, 8, 120),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Throws_When_Sets_Not_Positive()
    {
        var (context, routine, exercise, _) = SeedDraftRoutineWithExercise();
        var handler = new UpdateExerciseInWorkoutRoutineHandler(context);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                new UpdateExerciseInWorkoutRoutineCommand(routine.Id, exercise.Id, 0, 8, 120),
                CancellationToken.None));
    }
}
