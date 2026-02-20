using FitRos.Application.Features.WorkoutRoutines.MoveExerciseInWorkoutRoutine;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using FitRos.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class MoveExerciseInWorkoutRoutineHandlerTests
{
    private FitRosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new FitRosDbContext(options);
    }

    private static WorkoutRoutine CreateRoutineWithThreeExercises(
        out Guid ex1, out Guid ex2, out Guid ex3)
    {
        var routine = WorkoutRoutine.Create("Test", "Desc");

        ex1 = Guid.NewGuid();
        ex2 = Guid.NewGuid();
        ex3 = Guid.NewGuid();

        routine.AddExercise(ex1, 1, 3, 10, 60);
        routine.AddExercise(ex2, 2, 3, 10, 60);
        routine.AddExercise(ex3, 3, 3, 10, 60);

        return routine;
    }

    private static void AssertOrders(
        WorkoutRoutine updatedRoutine,
        params (Guid exerciseId, int order)[] expected)
    {
        foreach (var (exerciseId, order) in expected)
        {
            updatedRoutine.Exercises.Should()
                .ContainSingle(e => e.ExerciseId == exerciseId && e.Order == order);
        }

        // Ensure no duplicates and contiguous range 1..N
        updatedRoutine.Exercises.Select(e => e.Order)
            .Should()
            .BeEquivalentTo(Enumerable.Range(1, updatedRoutine.Exercises.Count));

        updatedRoutine.Exercises.Select(e => e.Order)
            .Should()
            .OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Handle_Should_Move_Exercise_Up_Correctly()
    {
        var context = CreateDbContext();

        var routine = CreateRoutineWithThreeExercises(out var ex1, out var ex2, out var ex3);

        context.WorkoutRoutines.Add(routine);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new MoveExerciseInWorkoutRoutineHandler(context);

        var command = new MoveExerciseInWorkoutRoutineCommand(routine.Id, ex3, 1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();

        var updatedRoutine = await context.WorkoutRoutines
            .Include(r => r.Exercises)
            .FirstAsync(r => r.Id == routine.Id);

        AssertOrders(updatedRoutine,
            (ex3, 1),
            (ex1, 2),
            (ex2, 3));
    }

    [Fact]
    public async Task Handle_Should_Move_Exercise_Down_Correctly()
    {
        var context = CreateDbContext();

        var routine = CreateRoutineWithThreeExercises(out var ex1, out var ex2, out var ex3);

        context.WorkoutRoutines.Add(routine);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new MoveExerciseInWorkoutRoutineHandler(context);

        var command = new MoveExerciseInWorkoutRoutineCommand(routine.Id, ex1, 3);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();

        var updatedRoutine = await context.WorkoutRoutines
            .Include(r => r.Exercises)
            .FirstAsync(r => r.Id == routine.Id);

        AssertOrders(updatedRoutine,
            (ex2, 1),
            (ex3, 2),
            (ex1, 3));
    }

    [Fact]
    public async Task Handle_Should_Not_Change_When_NewOrder_Is_Same_As_CurrentOrder()
    {
        var context = CreateDbContext();

        var routine = WorkoutRoutine.Create("Test", "Desc");
        var ex1 = Guid.NewGuid();
        var ex2 = Guid.NewGuid();

        routine.AddExercise(ex1, 1, 3, 10, 60);
        routine.AddExercise(ex2, 2, 3, 10, 60);

        context.WorkoutRoutines.Add(routine);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new MoveExerciseInWorkoutRoutineHandler(context);

        var command = new MoveExerciseInWorkoutRoutineCommand(routine.Id, ex1, 1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();

        var updatedRoutine = await context.WorkoutRoutines
            .Include(r => r.Exercises)
            .FirstAsync(r => r.Id == routine.Id);

        AssertOrders(updatedRoutine,
            (ex1, 1),
            (ex2, 2));
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Routine_Not_Found()
    {
        var context = CreateDbContext();
        var handler = new MoveExerciseInWorkoutRoutineHandler(context);

        var command = new MoveExerciseInWorkoutRoutineCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1);

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Workout routine not found.");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Exercise_Not_In_Routine()
    {
        var context = CreateDbContext();

        var routine = WorkoutRoutine.Create("Test", "Desc");
        context.WorkoutRoutines.Add(routine);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new MoveExerciseInWorkoutRoutineHandler(context);

        var command = new MoveExerciseInWorkoutRoutineCommand(
            routine.Id,
            Guid.NewGuid(),
            1);

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Exercise not found in routine.");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_NewOrder_Is_Less_Than_Or_Equal_To_Zero()
    {
        var context = CreateDbContext();

        var routine = WorkoutRoutine.Create("Test", "Desc");
        var ex1 = Guid.NewGuid();

        routine.AddExercise(ex1, 1, 3, 10, 60);

        context.WorkoutRoutines.Add(routine);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new MoveExerciseInWorkoutRoutineHandler(context);

        var command = new MoveExerciseInWorkoutRoutineCommand(routine.Id, ex1, 0);

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("New order must be greater than zero.");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_NewOrder_Exceeds_Exercise_Count()
    {
        var context = CreateDbContext();

        var routine = WorkoutRoutine.Create("Test", "Desc");
        var ex1 = Guid.NewGuid();

        routine.AddExercise(ex1, 1, 3, 10, 60);

        context.WorkoutRoutines.Add(routine);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new MoveExerciseInWorkoutRoutineHandler(context);

        var command = new MoveExerciseInWorkoutRoutineCommand(routine.Id, ex1, 2);

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("New order exceeds the number of exercises.");
    }

    [Fact]
    public async Task Handle_Should_Not_Fail_When_Only_One_Exercise()
    {
        var context = CreateDbContext();

        var routine = WorkoutRoutine.Create("Test", "Desc");
        var ex1 = Guid.NewGuid();

        routine.AddExercise(ex1, 1, 3, 10, 60);

        context.WorkoutRoutines.Add(routine);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new MoveExerciseInWorkoutRoutineHandler(context);

        var command = new MoveExerciseInWorkoutRoutineCommand(routine.Id, ex1, 1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();

        var updatedRoutine = await context.WorkoutRoutines
            .Include(r => r.Exercises)
            .FirstAsync(r => r.Id == routine.Id);

        AssertOrders(updatedRoutine, (ex1, 1));
    }

    [Fact]
    public async Task Handle_Should_Not_Leave_Temporary_Order_Values()
    {
        var context = CreateDbContext();

        var routine = CreateRoutineWithThreeExercises(out var ex1, out var ex2, out var ex3);

        context.WorkoutRoutines.Add(routine);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new MoveExerciseInWorkoutRoutineHandler(context);

        var command = new MoveExerciseInWorkoutRoutineCommand(routine.Id, ex3, 1);

        await handler.Handle(command, CancellationToken.None);

        var updatedRoutine = await context.WorkoutRoutines
            .Include(r => r.Exercises)
            .FirstAsync(r => r.Id == routine.Id);

        updatedRoutine.Exercises.Should().OnlyContain(e => e.Order > 0);
    }
}