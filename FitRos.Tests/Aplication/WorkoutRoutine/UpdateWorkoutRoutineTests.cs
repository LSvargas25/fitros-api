using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FitRos.Application.Features.WorkoutRoutines.UpdateWorkoutRoutine;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class UpdateWorkoutRoutineTests
{
    private FitRosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FitRosDbContext(options);
    }

    [Fact]
    public async Task Should_Update_Routine_When_In_Draft_State()
    {
        var context = CreateContext();

        var routine = WorkoutRoutine.Create("Push Day", "Chest");
        context.Add(routine);
        await context.SaveChangesAsync();

        var handler = new UpdateWorkoutRoutineHandler(context);

        var command = new UpdateWorkoutRoutineCommand(
            "Upper Push",
            "Chest and shoulders"
        );

        var result = await handler.Handle(
            routine.Id,
            command,
            CancellationToken.None);

        result.Should().BeTrue();
        routine.Name.Should().Be("Upper Push");
        routine.Description.Should().Be("Chest and shoulders");
    }

    [Fact]
    public async Task Should_Return_False_When_Routine_Does_Not_Exist()
    {
        var context = CreateContext();
        var handler = new UpdateWorkoutRoutineHandler(context);

        var command = new UpdateWorkoutRoutineCommand(
            "New Name",
            "New Desc"
        );

        var result = await handler.Handle(
            Guid.NewGuid(),
            command,
            CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Throw_When_Routine_Is_Published()
    {
        var context = CreateContext();

        var routine = WorkoutRoutine.Create("Push Day", "Chest");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 60);
        routine.Publish();

        context.Add(routine);
        await context.SaveChangesAsync();

        var handler = new UpdateWorkoutRoutineHandler(context);

        var command = new UpdateWorkoutRoutineCommand(
            "New Name",
            "New Desc"
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(routine.Id, command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Name_Is_Empty()
    {
        var context = CreateContext();

        var routine = WorkoutRoutine.Create("Push Day", "Chest");
        context.Add(routine);
        await context.SaveChangesAsync();

        var handler = new UpdateWorkoutRoutineHandler(context);

        var command = new UpdateWorkoutRoutineCommand(
            "",
            "New Desc"
        );

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(routine.Id, command, CancellationToken.None));
    }
}
