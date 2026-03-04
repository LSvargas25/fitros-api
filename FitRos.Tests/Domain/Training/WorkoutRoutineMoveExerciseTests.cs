using System;
using System.Linq;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Domain.Training;

public class WorkoutRoutineMoveExerciseTests
{
    private static WorkoutRoutine CreateRoutineWithThreeExercises(
        out Guid ex1, out Guid ex2, out Guid ex3)
    {
        var gymId = Guid.NewGuid();
        var routine = WorkoutRoutine.Create(gymId, "Test", "Desc");

        ex1 = Guid.NewGuid();
        ex2 = Guid.NewGuid();
        ex3 = Guid.NewGuid();

        routine.AddExercise(ex1, 1, 3, 10, 60);
        routine.AddExercise(ex2, 2, 3, 10, 60);
        routine.AddExercise(ex3, 3, 3, 10, 60);

        return routine;
    }

    private static int GetOrder(WorkoutRoutine routine, Guid exerciseId)
        => routine.Exercises.Single(e => e.ExerciseId == exerciseId).Order;

    private static void AssertOrders(
        WorkoutRoutine routine,
        params (Guid exerciseId, int order)[] expected)
    {
        foreach (var (exerciseId, order) in expected)
        {
            routine.Exercises.Should()
                .ContainSingle(e => e.ExerciseId == exerciseId && e.Order == order);
        }

        routine.Exercises.Select(e => e.Order)
            .Should()
            .BeEquivalentTo(Enumerable.Range(1, routine.Exercises.Count));

        routine.Exercises.Select(e => e.Order)
            .Should()
            .OnlyHaveUniqueItems();
    }

    [Fact]
    public void MoveExercise_Should_Move_Up_Correctly()
    {
        var routine = CreateRoutineWithThreeExercises(out var ex1, out var ex2, out var ex3);
        var originalOrder = GetOrder(routine, ex3);

        routine.MoveExercise(ex3, newOrder: 1, originalOrder: originalOrder);

        AssertOrders(routine,
            (ex3, 1),
            (ex1, 2),
            (ex2, 3));
    }

    [Fact]
    public void MoveExercise_Should_Move_Down_Correctly()
    {
        var routine = CreateRoutineWithThreeExercises(out var ex1, out var ex2, out var ex3);
        var originalOrder = GetOrder(routine, ex1);

        routine.MoveExercise(ex1, newOrder: 3, originalOrder: originalOrder);

        AssertOrders(routine,
            (ex2, 1),
            (ex3, 2),
            (ex1, 3));
    }

    [Fact]
    public void MoveExercise_Should_Not_Change_When_NewOrder_Equals_OriginalOrder()
    {
        var routine = CreateRoutineWithThreeExercises(out var ex1, out var ex2, out var ex3);
        var originalOrder = GetOrder(routine, ex2);

        routine.MoveExercise(ex2, newOrder: originalOrder, originalOrder: originalOrder);

        AssertOrders(routine,
            (ex1, 1),
            (ex2, 2),
            (ex3, 3));
    }

    [Fact]
    public void MoveExercise_Should_Move_To_Last_Position_Correctly()
    {
        var routine = CreateRoutineWithThreeExercises(out var ex1, out var ex2, out var ex3);
        var originalOrder = GetOrder(routine, ex2);

        routine.MoveExercise(ex2, newOrder: 3, originalOrder: originalOrder);

        AssertOrders(routine,
            (ex1, 1),
            (ex3, 2),
            (ex2, 3));
    }

    [Fact]
    public void MoveExercise_Should_Move_To_First_Position_Correctly()
    {
        var routine = CreateRoutineWithThreeExercises(out var ex1, out var ex2, out var ex3);
        var originalOrder = GetOrder(routine, ex2);

        routine.MoveExercise(ex2, newOrder: 1, originalOrder: originalOrder);

        AssertOrders(routine,
            (ex2, 1),
            (ex1, 2),
            (ex3, 3));
    }

    [Fact]
    public void MoveExercise_Should_Throw_When_Exercise_Not_Found()
    {
        var gymId = Guid.NewGuid();
        var routine = WorkoutRoutine.Create(gymId, "Test", "Desc");

        var ex1 = Guid.NewGuid();
        routine.AddExercise(ex1, 1, 3, 10, 60);

        var act = () => routine.MoveExercise(Guid.NewGuid(), newOrder: 1, originalOrder: 1);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Exercise not found in routine.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MoveExercise_Should_Throw_When_NewOrder_Is_Less_Than_Or_Equal_To_Zero(int invalidOrder)
    {
        var gymId = Guid.NewGuid();
        var routine = WorkoutRoutine.Create(gymId, "Test", "Desc");

        var ex1 = Guid.NewGuid();
        routine.AddExercise(ex1, 1, 3, 10, 60);

        var originalOrder = GetOrder(routine, ex1);

        var act = () => routine.MoveExercise(ex1, newOrder: invalidOrder, originalOrder: originalOrder);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("New order must be greater than zero.");
    }

    [Fact]
    public void MoveExercise_Should_Throw_When_NewOrder_Exceeds_Exercise_Count()
    {
        var gymId = Guid.NewGuid();
        var routine = WorkoutRoutine.Create(gymId, "Test", "Desc");

        var ex1 = Guid.NewGuid();
        routine.AddExercise(ex1, 1, 3, 10, 60);

        var originalOrder = GetOrder(routine, ex1);

        var act = () => routine.MoveExercise(ex1, newOrder: 2, originalOrder: originalOrder);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("New order exceeds the number of exercises.");
    }

    [Fact]
    public void MoveExercise_Should_Throw_When_Routine_Is_Not_Draft()
    {
        var gymId = Guid.NewGuid();
        var routine = WorkoutRoutine.Create(gymId, "Test", "Desc");

        var ex1 = Guid.NewGuid();
        routine.AddExercise(ex1, 1, 3, 10, 60);

        routine.Publish();

        var act = () => routine.MoveExercise(ex1, newOrder: 1, originalOrder: 1);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Routine can only be modified in Draft state.");
    }
}