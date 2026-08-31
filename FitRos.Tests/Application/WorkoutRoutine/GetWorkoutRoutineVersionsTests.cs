using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineVersions;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines;

// PASO 2.2 — the /{id}/versions endpoint lists every routine in the group,
// newest version first, with each version's own status. The existing API tests
// only covered the single-version happy path + cross-tenant denial.
public class GetWorkoutRoutineVersionsTests
{
    [Fact]
    public async Task Lists_every_version_in_the_group_newest_first()
    {
        var container = TestDbContextFactory.CreateContainer();
        var context = container.Context;
        var gymId = container.CurrentUser.GymId!.Value;

        var v1 = WorkoutRoutine.Create(gymId, "Push Day", "Chest");
        v1.AddExercise(Guid.NewGuid(), 1, 3, 10, 60);
        v1.Publish();
        var v2 = v1.CreateNewVersion();
        v2.Publish();
        var v3 = v2.CreateNewVersion(); // stays Draft

        context.WorkoutRoutines.AddRange(v1, v2, v3);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetWorkoutRoutineVersionsHandler(context)
            .Handle(new GetWorkoutRoutineVersionsQuery(v2.Id), CancellationToken.None);

        result.Select(r => r.Version).Should().ContainInOrder(3, 2, 1);
        result.Should().OnlyContain(r => r.RoutineGroupId == v1.RoutineGroupId);
        result.Single(r => r.Version == 1).Status.Should().Be(RoutineStatus.Published);
        result.Single(r => r.Version == 2).Status.Should().Be(RoutineStatus.Published);
        result.Single(r => r.Version == 3).Status.Should().Be(RoutineStatus.Draft);
    }

    [Fact]
    public async Task A_single_version_routine_returns_exactly_itself()
    {
        var container = TestDbContextFactory.CreateContainer();
        var context = container.Context;
        var gymId = container.CurrentUser.GymId!.Value;

        var only = WorkoutRoutine.Create(gymId, "Solo", "x");
        context.WorkoutRoutines.Add(only);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetWorkoutRoutineVersionsHandler(context)
            .Handle(new GetWorkoutRoutineVersionsQuery(only.Id), CancellationToken.None);

        result.Should().ContainSingle().Which.Id.Should().Be(only.Id);
    }

    [Fact]
    public async Task Unknown_routine_id_is_not_found()
    {
        var container = TestDbContextFactory.CreateContainer();
        var context = container.Context;

        var act = async () => await new GetWorkoutRoutineVersionsHandler(context)
            .Handle(new GetWorkoutRoutineVersionsQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
