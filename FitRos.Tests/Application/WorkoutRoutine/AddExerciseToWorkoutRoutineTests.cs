using FitRos.Application.Features.WorkoutRoutines.AddExerciseToWorkoutRoutine;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class AddExerciseToWorkoutRoutineTests
{
    [Fact]
    public async Task Handle_Should_Add_Exercise_To_Routine()
    {
        var container = TestDbContextFactory.CreateContainer();
        var context = container.Context;
        var gymId = container.CurrentUser.GymId!.Value;

        var routine = WorkoutRoutine.Create(
            gymId,
            "Test Routine",
            "Description");

        var exercise = Exercise.Create(
     "Bench Press",
     "Chest exercise",
     MuscleGroup.Chest,
     gymId);

        context.WorkoutRoutines.Add(routine);
        context.Exercises.Add(exercise);

        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AddExerciseToWorkoutRoutineHandler(context);

        var command = new AddExerciseToWorkoutRoutineCommand(
            routine.Id,
            exercise.Id,
            1,
            4,
            10,
            90);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();

        var updatedRoutine = await context.WorkoutRoutines
            .Include(r => r.Exercises)
            .FirstAsync(r => r.Id == routine.Id);

        updatedRoutine.Exercises.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Adding_Duplicate_Exercise()
    {
        var container = TestDbContextFactory.CreateContainer();
        var context = container.Context;
        var gymId = container.CurrentUser.GymId!.Value;

        var routine = WorkoutRoutine.Create(
            gymId,
            "Test Routine",
            "Description");

        var exercise = Exercise.Create(
            "Bench Press",
            "Chest exercise",
            MuscleGroup.Chest,
            gymId);

        context.WorkoutRoutines.Add(routine);
        context.Exercises.Add(exercise);

        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AddExerciseToWorkoutRoutineHandler(context);

        var command = new AddExerciseToWorkoutRoutineCommand(
            routine.Id,
            exercise.Id,
            1,
            4,
            10,
            90);

        await handler.Handle(command, CancellationToken.None);

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("This exercise is already part of the routine.");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Routine_Is_Published()
    {
        var container = TestDbContextFactory.CreateContainer();
        var context = container.Context;
        var gymId = container.CurrentUser.GymId!.Value;

        var routine = WorkoutRoutine.Create(
            gymId,
            "Test Routine",
            "Description");

        var firstExercise = Exercise.Create(
            "Bench Press",
            "Chest exercise",
            MuscleGroup.Chest);

        var secondExercise = Exercise.Create(
            "Lat Pulldown",
            "Back exercise",
            MuscleGroup.Back);

        context.WorkoutRoutines.Add(routine);
        context.Exercises.Add(firstExercise);
        context.Exercises.Add(secondExercise);

        await context.SaveChangesAsync(CancellationToken.None);

        routine.AddExercise(firstExercise.Id, 1, 4, 10, 90);
        routine.Publish();

        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AddExerciseToWorkoutRoutineHandler(context);

        var command = new AddExerciseToWorkoutRoutineCommand(
            routine.Id,
            secondExercise.Id,
            2,
            3,
            12,
            60);

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Routine can only be modified in Draft state.");
    }
}