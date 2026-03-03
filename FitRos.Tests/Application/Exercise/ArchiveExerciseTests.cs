using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.Exercises.ArchiveExercise;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.Exercises;

public class ArchiveExerciseHandlerTests
{
    private static FitRosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var mockUser = new Mock<ICurrentUser>();
        mockUser.Setup(x => x.UserId).Returns(Guid.NewGuid());
        mockUser.Setup(x => x.Role).Returns(UserRole.Admin);
        mockUser.Setup(x => x.IsAuthenticated).Returns(true);

        return new FitRosDbContext(options, mockUser.Object);
    }

    [Fact]
    public async Task Should_Archive_Exercise_When_Exists()
    {
        var context = CreateDbContext();

        var exercise = Exercise.Create(
            "Bench Press",
            "Chest",
            MuscleGroup.Chest);

        context.Add(exercise);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ArchiveExerciseHandler(context);

        await handler.Handle(
            new ArchiveExerciseCommand(exercise.Id),
            CancellationToken.None);

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
          .ThrowAsync<NotFoundException>()
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