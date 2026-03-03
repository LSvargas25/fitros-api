using FitRos.Application.Features.Exercises.UpdateExercise;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Exercises;

public class UpdateExerciseHandlerTests
{
    [Fact]
    public async Task Should_Update_Exercise_When_Data_Is_Valid()
    {
        var context = TestDbContextFactory.Create();

        var exercise = Exercise.Create(
            "Bench Press",
            "Chest movement",
            MuscleGroup.Chest);

        context.Add(exercise);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateExerciseHandler(context);

        var command = new UpdateExerciseCommand(
            exercise.Id,
            "Incline Bench Press",
            "Upper chest focus",
            MuscleGroup.Chest);

        await handler.Handle(command, CancellationToken.None);

        var updated = await context.Exercises
            .FirstAsync(x => x.Id == exercise.Id);

        updated.Name.Should().Be("Incline Bench Press");
        updated.Description.Should().Be("Upper chest focus");
        updated.NormalizedName.Should().Be("incline bench press");
    }

    [Fact]
    public async Task Should_Throw_When_Exercise_Not_Found()
    {
        var context = TestDbContextFactory.Create();
        var handler = new UpdateExerciseHandler(context);

        var command = new UpdateExerciseCommand(
            Guid.NewGuid(),
            "Test",
            "Desc",
            MuscleGroup.Chest);

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
           .ThrowAsync<NotFoundException>()
            .WithMessage("Exercise not found.");
    }

    [Fact]
    public async Task Should_Throw_When_Name_Already_Exists()
    {
        var context = TestDbContextFactory.Create();

        var exercise1 = Exercise.Create(
            "Bench Press",
            "Chest",
            MuscleGroup.Chest);

        var exercise2 = Exercise.Create(
            "Squat",
            "Legs",
            MuscleGroup.Legs);

        context.AddRange(exercise1, exercise2);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateExerciseHandler(context);

        var command = new UpdateExerciseCommand(
            exercise2.Id,
            "Bench Press",
            "New desc",
            MuscleGroup.Legs);

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Exercise 'Bench Press' already exists.");
    }

    [Fact]
    public async Task Should_Throw_When_Exercise_Is_Archived()
    {
        var context = TestDbContextFactory.Create();

        var exercise = Exercise.Create(
            "Bench Press",
            "Chest",
            MuscleGroup.Chest);

        exercise.Archive();

        context.Add(exercise);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateExerciseHandler(context);

        var command = new UpdateExerciseCommand(
            exercise.Id,
            "New Name",
            "New Desc",
            MuscleGroup.Chest);

        Func<Task> act = async () =>
            await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Archived exercises cannot be modified.");
    }
}