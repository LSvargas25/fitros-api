using FitRos.Application.Features.Exercises.GetExerciseById;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Application.Tests.Features.Exercises.GetExerciseById;

public class GetExerciseByIdTests
{ 
    [Fact]
    public async Task Handle_ShouldReturnExercise_WhenExerciseExists()
    {
        // Arrange
        var context = TestDbContextFactory.Create();

        var exercise = Exercise.Create(
            "Bench Press",
            "Chest exercise",
            MuscleGroup.Chest);

        context.Exercises.Add(exercise);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetExerciseByIdHandler(context);

        // Act
        var result = await handler.Handle(exercise.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(exercise.Id, result!.Id);
        Assert.Equal("Bench Press", result.Name);
        Assert.Equal("Chest exercise", result.Description);
        Assert.Equal(MuscleGroup.Chest, result.Category);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenExerciseDoesNotExist()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var handler = new GetExerciseByIdHandler(context);

        // Act
        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
