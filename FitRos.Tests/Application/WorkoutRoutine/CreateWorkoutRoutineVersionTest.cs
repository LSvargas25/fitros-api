using FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutineVersion;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class CreateWorkoutRoutineVersionTests
{
   

    [Fact]
    public async Task Should_Not_Create_New_Version_When_Draft_Already_Exists()
    {
        var context = TestDbContextFactory.Create();

        var v1 = WorkoutRoutine.Create("Push Day", "Chest");
        v1.AddExercise(Guid.NewGuid(), 1, 3, 10, 60);
        v1.Publish();

        var v2 = v1.CreateNewVersion();

        context.WorkoutRoutines.AddRange(v1, v2);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateWorkoutRoutineVersionHandler(context);

        var act = async () =>
            await handler.Handle(
                new CreateWorkoutRoutineVersionCommand(v1.Id),
                CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("A draft version already exists for this routine group.");
    }
}