using FitRos.Application.Features.WorkoutRoutines.PublishWorkoutRoutine;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines.PublishWorkoutRoutine;

public class PublishWorkoutRoutineHandlerTests
{
    [Fact]
    public async Task Handle_Should_Throw_When_Routine_Is_Not_Latest_Version()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var v1 = WorkoutRoutine.Create(gymId, "Push Day", "Chest");
        v1.AddExercise(Guid.NewGuid(), 1, 3, 10, 60);
        v1.Publish();

        var v2 = v1.CreateNewVersion();
        v2.AddExercise(Guid.NewGuid(), 2, 3, 10, 60);

        context.WorkoutRoutines.Add(v1);
        context.WorkoutRoutines.Add(v2);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new PublishWorkoutRoutineHandler(context);

        var command = new PublishWorkoutRoutineCommand(v1.Id);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Only the latest version can be published.");
    }

    [Fact]
    public async Task Handle_Should_Publish_When_Routine_Is_Latest_Version()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var v1 = WorkoutRoutine.Create(gymId, "Push Day", "Chest");
        v1.AddExercise(Guid.NewGuid(), 1, 3, 10, 60);
        v1.Publish();

        var v2 = v1.CreateNewVersion();
        v2.AddExercise(Guid.NewGuid(), 2, 3, 10, 60);

        context.WorkoutRoutines.Add(v1);
        context.WorkoutRoutines.Add(v2);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new PublishWorkoutRoutineHandler(context);

        await handler.Handle(new PublishWorkoutRoutineCommand(v2.Id), CancellationToken.None);

        var reloaded = await context.WorkoutRoutines.FirstAsync(r => r.Id == v2.Id);
        reloaded.Status.Should().Be(FitRos.Domain.Enums.RoutineStatus.Published);
    }
}