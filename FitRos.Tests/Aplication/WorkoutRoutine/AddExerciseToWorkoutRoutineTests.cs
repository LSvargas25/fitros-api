using System;
using System.Threading;
using System.Threading.Tasks;
using FitRos.Application.Features.WorkoutRoutines.AddExerciseToWorkoutRoutine;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.Enums;
using FitRos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines
{
    public class AddExerciseToWorkoutRoutineTests
    {
        private FitRosDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<FitRosDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new FitRosDbContext(options);
        }

        [Fact]
        public async Task Handle_Should_Add_Exercise_To_Routine()
        {
            // Arrange
            var context = CreateDbContext();

            var routine = WorkoutRoutine.Create(
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
    SuggestedRestSeconds: 90
);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            var updatedRoutine = await context.WorkoutRoutines
                .Include(r => r.Exercises)
                .FirstAsync();

            updatedRoutine.Exercises.Should().HaveCount(1);
        }

        [Fact]
        public async Task Handle_Should_Throw_When_Adding_Duplicate_Exercise()
        {
            // Arrange
            var context = CreateDbContext();

            var routine = WorkoutRoutine.Create(
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
      SuggestedRestSeconds: 90
  );

            // First insert
            await handler.Handle(command, CancellationToken.None);

            // Act
            Func<Task> act = async () =>
                await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should()
                .ThrowAsync<DomainException>()
                .WithMessage("This exercise is already part of the routine.");
        }



        [Fact]
        public async Task Handle_Should_Throw_When_Routine_Is_Published()
        {
            // Arrange
            var context = CreateDbContext();

            var routine = WorkoutRoutine.Create("Test Routine", "Description");

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

            // Add first exercise and publish routine
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
                SuggestedRestSeconds: 60
            );

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should()
                .ThrowAsync<DomainException>()
                .WithMessage("Routine can only be modified in Draft state.");
        }
    }
}
