using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FitRos.Application.Features.WorkoutRoutines.PublishWorkoutRoutine;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class PublishWorkoutRoutineTests
{
    private FitRosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FitRosDbContext(options);
    }

    [Fact]
    public async Task Should_Publish_Routine_When_Status_Is_Draft()
    {
        var context = CreateContext();

        var routine = WorkoutRoutine.Create("Push Day", "Chest workout");

        routine.AddExercise(
            Guid.NewGuid(),
            4,
            10,
            1,
            60
        );

        var versionBeforePublish = routine.Version;

        context.Add(routine);
        await context.SaveChangesAsync();

        var handler = new PublishWorkoutRoutineHandler(context);
        var command = new PublishWorkoutRoutineCommand(routine.Id);

        await handler.Handle(command, CancellationToken.None);

        routine.Status.Should().Be(RoutineStatus.Published);
        routine.Version.Should().Be(versionBeforePublish + 1);
         
    }


    [Fact]
    public async Task Should_Return_False_When_Routine_Does_Not_Exist()
    {
        var context = CreateContext();
        var handler = new PublishWorkoutRoutineHandler(context);

        var command = new PublishWorkoutRoutineCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
    handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_InvalidOperationException_When_Routine_Is_Already_Published()
    {
        var context = CreateContext();

        var routine = WorkoutRoutine.Create("Push Day", "Chest workout");

        routine.AddExercise(
            Guid.NewGuid(),
            4,
            10,
            1,
            60
        );

        routine.Publish(); 

        context.Add(routine);
        await context.SaveChangesAsync();

        var handler = new PublishWorkoutRoutineHandler(context);
        var command = new PublishWorkoutRoutineCommand(routine.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
