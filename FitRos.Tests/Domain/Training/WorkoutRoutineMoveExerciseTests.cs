using FitRos.Domain.Entities.Training;
using FitRos.Domain.Common;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Domain.Training;

public class WorkoutRoutineMoveExerciseTests
{
    [Fact]
    public void MoveExercise_Should_Move_Up_Correctly()
    {
        // Arrange
        var routine = WorkoutRoutine.Create("Test", "Desc");

        var ex1 = Guid.NewGuid();
        var ex2 = Guid.NewGuid();
        var ex3 = Guid.NewGuid();

        routine.AddExercise(ex1, 1, 3, 10, 60);
        routine.AddExercise(ex2, 2, 3, 10, 60);
        routine.AddExercise(ex3, 3, 3, 10, 60);

        // Act
        routine.MoveExercise(ex3, 1);

        // Assert
        routine.Exercises.Should()
            .ContainSingle(e => e.ExerciseId == ex3 && e.Order == 1);

        routine.Exercises.Should()
            .ContainSingle(e => e.ExerciseId == ex1 && e.Order == 2);

        routine.Exercises.Should()
            .ContainSingle(e => e.ExerciseId == ex2 && e.Order == 3);
    }

    [Fact]
    public void MoveExercise_Should_Move_Down_Correctly()
    {
        // Arrange
        var routine = WorkoutRoutine.Create("Test", "Desc");

        var ex1 = Guid.NewGuid();
        var ex2 = Guid.NewGuid();
        var ex3 = Guid.NewGuid();

        routine.AddExercise(ex1, 1, 3, 10, 60);
        routine.AddExercise(ex2, 2, 3, 10, 60);
        routine.AddExercise(ex3, 3, 3, 10, 60);

        // Act
        routine.MoveExercise(ex1, 3);

        // Assert
        routine.Exercises.Should()
            .ContainSingle(e => e.ExerciseId == ex2 && e.Order == 1);

        routine.Exercises.Should()
            .ContainSingle(e => e.ExerciseId == ex3 && e.Order == 2);

        routine.Exercises.Should()
            .ContainSingle(e => e.ExerciseId == ex1 && e.Order == 3);
    }

    [Fact]
    public void MoveExercise_Should_Throw_When_Exercise_Not_Found()
    {
        var routine = WorkoutRoutine.Create("Test", "Desc");

        var ex1 = Guid.NewGuid();
        routine.AddExercise(ex1, 1, 3, 10, 60);

        var act = () => routine.MoveExercise(Guid.NewGuid(), 1);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Exercise not found in routine.");
    }

    [Fact]
    public void MoveExercise_Should_Throw_When_Routine_Is_Published()
    {
        var routine = WorkoutRoutine.Create("Test", "Desc");

        var ex1 = Guid.NewGuid();
        routine.AddExercise(ex1, 1, 3, 10, 60);

        routine.Publish();

        var act = () => routine.MoveExercise(ex1, 1);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Routine can only be modified in Draft state.");
    }

    [Fact]
    public void MoveExercise_Should_Throw_When_Order_Is_Invalid()
    {
        var routine = WorkoutRoutine.Create("Test", "Desc");

        var ex1 = Guid.NewGuid();
        routine.AddExercise(ex1, 1, 3, 10, 60);

        var act = () => routine.MoveExercise(ex1, 2);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("New order exceeds the number of exercises.");
    }
}
