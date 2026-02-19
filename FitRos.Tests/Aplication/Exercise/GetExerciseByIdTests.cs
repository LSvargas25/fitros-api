using FitRos.Application.Features.Exercises.GetExerciseById;
using FitRos.Infrastructure.Persistence;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Application.Tests.Features.Exercises.GetExerciseById;

public class GetExerciseByIdTests
{
    private FitRosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FitRosDbContext(options);
    }

    [Fact]
    public async Task Handle_ShouldReturnExercise_WhenExerciseExists()
    {
        // Arrange
        var context = CreateDbContext();

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
        var context = CreateDbContext();
        var handler = new GetExerciseByIdHandler(context);

        // Act
        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
