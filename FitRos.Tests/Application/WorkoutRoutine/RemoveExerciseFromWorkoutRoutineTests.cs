 
using FitRos.Application.Features.WorkoutRoutines.RemoveExerciseFromWorkoutRoutine;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class RemoveExerciseFromWorkoutRoutineTests
{
    [Fact]
    public async Task Handle_Should_Remove_Exercise_From_Routine()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var routine = WorkoutRoutine.Create(gymId, "Test Routine", "Desc");

        var exerciseId = Guid.NewGuid();

        routine.AddExercise(exerciseId, 1, 3, 10, 60);

        context.WorkoutRoutines.Add(routine);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new RemoveExerciseFromWorkoutRoutineHandler(context);

        var command = new RemoveExerciseFromWorkoutRoutineCommand(
            routine.Id,
            exerciseId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();

        var updatedRoutine = await context.WorkoutRoutines
            .Include(r => r.Exercises)
            .FirstAsync(r => r.Id == routine.Id);

        updatedRoutine.Exercises.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Routine_Not_Found()
    {
        var context = TestDbContextFactory.Create();
        var handler = new RemoveExerciseFromWorkoutRoutineHandler(context);

        var command = new RemoveExerciseFromWorkoutRoutineCommand(
            Guid.NewGuid(),
            Guid.NewGuid());

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Workout routine not found.");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Exercise_Not_In_Routine()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();
        var routine = WorkoutRoutine.Create(gymId, "Test Routine", "Desc");
        context.WorkoutRoutines.Add(routine);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new RemoveExerciseFromWorkoutRoutineHandler(context);

        var command = new RemoveExerciseFromWorkoutRoutineCommand(
            routine.Id,
            Guid.NewGuid());

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Exercise not found in routine.");
    }
}
 
