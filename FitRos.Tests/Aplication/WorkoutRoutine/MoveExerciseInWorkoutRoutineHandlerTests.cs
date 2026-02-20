using FitRos.Application.Features.WorkoutRoutines.MoveExerciseInWorkoutRoutine;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Common;
using FitRos.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class MoveExerciseInWorkoutRoutineHandlerTests
{
    private FitRosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FitRosDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_Move_Exercise_Correctly()
    {
        // Arrange
        var context = CreateDbContext();

        var routine = WorkoutRoutine.Create("Test", "Desc");

        var ex1 = Guid.NewGuid();
        var ex2 = Guid.NewGuid();
        var ex3 = Guid.NewGuid();

        routine.AddExercise(ex1, 1, 3, 10, 60);
        routine.AddExercise(ex2, 2, 3, 10, 60);
        routine.AddExercise(ex3, 3, 3, 10, 60);

        context.WorkoutRoutines.Add(routine);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new MoveExerciseInWorkoutRoutineHandler(context);

        var command = new MoveExerciseInWorkoutRoutineCommand(
            routine.Id,
            ex3,
            1);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var updatedRoutine = await context.WorkoutRoutines
            .Include(r => r.Exercises)
            .FirstAsync(r => r.Id == routine.Id);

        updatedRoutine.Exercises.Should()
            .ContainSingle(e => e.ExerciseId == ex3 && e.Order == 1);

        updatedRoutine.Exercises.Should()
            .ContainSingle(e => e.ExerciseId == ex1 && e.Order == 2);

        updatedRoutine.Exercises.Should()
            .ContainSingle(e => e.ExerciseId == ex2 && e.Order == 3);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Routine_Not_Found()
    {
        // Arrange
        var context = CreateDbContext();
        var handler = new MoveExerciseInWorkoutRoutineHandler(context);

        var command = new MoveExerciseInWorkoutRoutineCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Workout routine not found.");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Exercise_Not_In_Routine()
    {
        // Arrange
        var context = CreateDbContext();

        var routine = WorkoutRoutine.Create("Test", "Desc");
        context.WorkoutRoutines.Add(routine);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new MoveExerciseInWorkoutRoutineHandler(context);

        var command = new MoveExerciseInWorkoutRoutineCommand(
            routine.Id,
            Guid.NewGuid(),
            1);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Exercise not found in routine.");
    }
}
