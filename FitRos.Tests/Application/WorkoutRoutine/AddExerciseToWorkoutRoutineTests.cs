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

namespace FitRos.Tests.Application.WorkoutRoutines
{
    public class AddExerciseToWorkoutRoutineTests
    {
        [Fact]
        public async Task Handle_Should_Add_Exercise_To_Routine()
        {
            var context = TestDbContextFactory.Create();

            var gymId = Guid.NewGuid();

            var routine = WorkoutRoutine.Create(
                gymId,
                "Test Routine",
                "Description");

            var exercise = Exercise.Create(
                "Bench Press",
                "Chest exercise",
                MuscleGroup.Chest);

            context.WorkoutRoutines.Add(routine);
            context.Exercises.Add(exercise);

            await context.SaveChangesAsync(CancellationToken.None);

            var handler = new AddExerciseToWorkoutRoutineHandler(context);

            var command = new AddExerciseToWorkoutRoutineCommand(
                WorkoutRoutineId: routine.Id,
                ExerciseId: exercise.Id,
                Order: 1,
                SuggestedSets: 4,
                SuggestedReps: 10,
                SuggestedRestSeconds: 90);

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();

            var updatedRoutine = await context.WorkoutRoutines
                .Include(r => r.Exercises)
                .FirstAsync();

            updatedRoutine.Exercises.Should().HaveCount(1);
        }

        [Fact]
        public async Task Handle_Should_Throw_When_Adding_Duplicate_Exercise()
        {
            var context = TestDbContextFactory.Create();

            var gymId = Guid.NewGuid();

            var routine = WorkoutRoutine.Create(
                gymId,
                "Test Routine",
                "Description");

            var exercise = Exercise.Create(
                "Bench Press",
                "Chest exercise",
                MuscleGroup.Chest);

            context.WorkoutRoutines.Add(routine);
            context.Exercises.Add(exercise);

            await context.SaveChangesAsync(CancellationToken.None);

            var handler = new AddExerciseToWorkoutRoutineHandler(context);

            var command = new AddExerciseToWorkoutRoutineCommand(
                WorkoutRoutineId: routine.Id,
                ExerciseId: exercise.Id,
                Order: 1,
                SuggestedSets: 4,
                SuggestedReps: 10,
                SuggestedRestSeconds: 90);

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
            var context = TestDbContextFactory.Create();

            var gymId = Guid.NewGuid();

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
                WorkoutRoutineId: routine.Id,
                ExerciseId: secondExercise.Id,
                Order: 2,
                SuggestedSets: 3,
                SuggestedReps: 12,
                SuggestedRestSeconds: 60);

            Func<Task> act = async () =>
                await handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<DomainException>()
                .WithMessage("Routine can only be modified in Draft state.");
        }
    }
}