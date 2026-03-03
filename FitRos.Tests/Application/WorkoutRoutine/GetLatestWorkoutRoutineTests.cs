using FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutineVersion;
using FitRos.Application.Features.WorkoutRoutines.GetLatestWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.PublishWorkoutRoutine;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines
{
    public class WorkoutRoutineVersioningFlowTests
    { 

        [Fact]
        public async Task Full_Versioning_Flow_Should_Work_Correctly()
        {
            var context = TestDbContextFactory.Create();

            // Step 1: Create v1
            var v1 = WorkoutRoutine.Create("Push Day", "Chest");
            v1.AddExercise(Guid.NewGuid(), 1, 3, 10, 60);

            context.WorkoutRoutines.Add(v1);
            await context.SaveChangesAsync(CancellationToken.None);

            // Step 2: Publish v1
            var publishHandler = new PublishWorkoutRoutineHandler(context);
            await publishHandler.Handle(
                new PublishWorkoutRoutineCommand(v1.Id),
                CancellationToken.None);

            var publishedV1 = await context.WorkoutRoutines.FirstAsync(r => r.Id == v1.Id);
            publishedV1.Status.Should().Be(FitRos.Domain.Enums.RoutineStatus.Published);
            publishedV1.Version.Should().Be(1); // Version does NOT change on publish

            // Step 3: Create v2 (Draft)
            var createVersionHandler = new CreateWorkoutRoutineVersionHandler(context);

            var response = await createVersionHandler.Handle(
                new CreateWorkoutRoutineVersionCommand(v1.Id),
                CancellationToken.None);

            response.Version.Should().Be(2);
            response.Status.Should().Be(FitRos.Domain.Enums.RoutineStatus.Draft);

            var v2Id = response.Id;

            // Step 4: Ensure second draft cannot be created
            var act = async () =>
                await createVersionHandler.Handle(
                    new CreateWorkoutRoutineVersionCommand(v1.Id),
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<DomainException>()
                .WithMessage("A draft version already exists for this routine group.");

            // Step 5: GET Latest should return v2
            var latestHandler = new GetLatestWorkoutRoutineHandler(context);

            var latest = await latestHandler.Handle(
                new GetLatestWorkoutRoutineQuery(v1.Id),
                CancellationToken.None);

            latest.Should().NotBeNull();
            latest!.Id.Should().Be(v2Id);
            latest.Version.Should().Be(2);
        }
    }
}