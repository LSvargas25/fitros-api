using FitRos.Application.Features.Exercises.ArchiveExercise;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Exercises;

public class ArchiveExerciseHandlerTests
{
    private static FitRosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FitRosDbContext(options);
    }

    [Fact]
    public async Task Should_Archive_Exercise_When_Exists()
    {
        // Arrange
        var context = CreateDbContext();

        var exercise = Exercise.Create(
            "Bench Press",
            "Chest",
            MuscleGroup.Chest);

        context.Add(exercise);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ArchiveExerciseHandler(context);

        // Act
        await handler.Handle(
            new ArchiveExerciseCommand(exercise.Id),
            CancellationToken.None);

        // Assert
        var archived = await context.Exercises
            .FirstAsync(x => x.Id == exercise.Id);

        archived.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Throw_When_Exercise_Not_Found()
    {
        var context = CreateDbContext();
        var handler = new ArchiveExerciseHandler(context);

        Func<Task> act = async () =>
            await handler.Handle(
                new ArchiveExerciseCommand(Guid.NewGuid()),
                CancellationToken.None);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Exercise not found.");
    }

    [Fact]
    public async Task Should_Throw_When_Exercise_Is_Already_Archived()
    {
        var context = CreateDbContext();

        var exercise = Exercise.Create(
            "Bench Press",
            "Chest",
            MuscleGroup.Chest);

        exercise.Archive();

        context.Add(exercise);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ArchiveExerciseHandler(context);

        Func<Task> act = async () =>
            await handler.Handle(
                new ArchiveExerciseCommand(exercise.Id),
                CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Exercise is already archived.");
    }
}
