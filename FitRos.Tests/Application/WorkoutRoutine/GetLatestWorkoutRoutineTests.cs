using FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutineVersion;
using FitRos.Application.Features.WorkoutRoutines.GetLatestWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.PublishWorkoutRoutine;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class WorkoutRoutineVersioningFlowTests
{
    [Fact]
    public async Task Full_Versioning_Flow_Should_Work_Correctly()
    {
        var container = TestDbContextFactory.CreateContainer();
        var context = container.Context;
        var gymId = container.CurrentUser.GymId!.Value;

        var v1 = WorkoutRoutine.Create(gymId, "Push Day", "Chest");
        v1.AddExercise(Guid.NewGuid(), 1, 3, 10, 60);

        context.WorkoutRoutines.Add(v1);
        await context.SaveChangesAsync(CancellationToken.None);

        var publishHandler = new PublishWorkoutRoutineHandler(context);

        await publishHandler.Handle(
            new PublishWorkoutRoutineCommand(v1.Id),
            CancellationToken.None);

        var publishedV1 = await context.WorkoutRoutines
       .IgnoreQueryFilters()
       .FirstAsync(r => r.Id == v1.Id);

        publishedV1.Status.Should().Be(FitRos.Domain.Enums.RoutineStatus.Published);
        publishedV1.Version.Should().Be(1);

        var createVersionHandler = new CreateWorkoutRoutineVersionHandler(context);

        var response = await createVersionHandler.Handle(
            new CreateWorkoutRoutineVersionCommand(v1.Id),
            CancellationToken.None);

        response.Version.Should().Be(2);
        response.Status.Should().Be(FitRos.Domain.Enums.RoutineStatus.Draft);

        var v2Id = response.Id;

        var act = async () =>
            await createVersionHandler.Handle(
                new CreateWorkoutRoutineVersionCommand(v1.Id),
                CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("A draft version already exists for this routine group.");

        var latestHandler = new GetLatestWorkoutRoutineHandler(context);

        var latest = await latestHandler.Handle(
            new GetLatestWorkoutRoutineQuery(v1.Id),
            CancellationToken.None);

        latest.Should().NotBeNull();
        latest!.Id.Should().Be(v2Id);
        latest.Version.Should().Be(2);
    }
}