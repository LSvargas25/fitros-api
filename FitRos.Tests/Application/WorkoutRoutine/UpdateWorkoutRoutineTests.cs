using FitRos.Application.Features.WorkoutRoutines.UpdateWorkoutRoutine;
using FitRos.Domain.Entities.Training;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class UpdateWorkoutRoutineTests
{
    [Fact]
    public async Task Should_Update_Routine_When_In_Draft_State()
    {
        var container = TestDbContextFactory.CreateContainer();
        var context = container.Context;
        var gymId = container.CurrentUser.GymId!.Value;

        var routine = WorkoutRoutine.Create(gymId, "Push Day", "Chest");

        context.WorkoutRoutines.Add(routine);
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
        var context = TestDbContextFactory.Create();
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
    public async Task Should_Throw_When_Name_Is_Empty()
    {
        var container = TestDbContextFactory.CreateContainer();
        var context = container.Context;
        var gymId = container.CurrentUser.GymId!.Value;

        var routine = WorkoutRoutine.Create(gymId, "Push Day", "Chest");

        context.WorkoutRoutines.Add(routine);
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