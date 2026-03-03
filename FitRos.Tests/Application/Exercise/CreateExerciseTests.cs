using FitRos.Application.Features.Exercises.CreateExercise;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Tests.Features.Exercises

{
    public class CreateExerciseTests
    {
      

        [Fact]
        public async Task Handle_ShouldCreateExercise_WhenExerciseDoesNotExist()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var handler = new CreateExerciseHandler(context);

            var command = new CreateExerciseCommand(
                "Bench Press",
                "Chest exercise",
                MuscleGroup.Chest);

            // Act
            var response = await handler.Handle(command, CancellationToken.None);

            // Assert
            var exerciseInDb = await context.Exercises
                .FirstOrDefaultAsync(x => x.Id == response.Id);

            Assert.NotNull(response);
            Assert.NotNull(exerciseInDb);
            Assert.Equal("Bench Press", exerciseInDb!.Name);
            Assert.Equal(MuscleGroup.Chest, exerciseInDb.Category);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenExerciseAlreadyExists()
        {
            // Arrange
            var context = TestDbContextFactory.Create();

            var existingExercise = Exercise.Create(
                "Bench Press",
                "Chest exercise",
                MuscleGroup.Chest);

            context.Exercises.Add(existingExercise);
            await context.SaveChangesAsync(CancellationToken.None);

            var handler = new CreateExerciseHandler(context);

            var command = new CreateExerciseCommand(
                "Bench Press", 
                "Another description",
                MuscleGroup.Chest);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(command, CancellationToken.None));
        }
    }
}
