using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FluentAssertions;

namespace FitRos.Tests.Domain.Training;

public class WorkoutRoutineTests
{
    [Fact]
    public void Should_Create_Routine_In_Draft_State()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest and triceps");

        routine.Status.Should().Be(RoutineStatus.Draft);
        routine.Version.Should().Be(1);
    }

    [Fact]
    public void Should_Not_Publish_Routine_Without_Exercises()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");

        var action = () => routine.Publish();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Should_Add_Exercise_In_Draft_State()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");

        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);

        routine.Exercises.Should().HaveCount(1);
    }

    [Fact]
    public void Should_Not_Allow_Modification_When_Published()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");

        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);
        routine.Publish();

        var action = () =>
            routine.AddExercise(Guid.NewGuid(), 2, 3, 12, 60);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Should_Create_New_Version_From_Published_Routine()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");

        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);
        routine.Publish();

        var newVersion = routine.CreateNewVersion();

        newVersion.Version.Should().Be(2);
        newVersion.Status.Should().Be(RoutineStatus.Draft);
        newVersion.Exercises.Should().HaveCount(1);
    }

    [Fact]
    public void Should_Not_Version_If_Not_Published()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");

        var action = () => routine.CreateNewVersion();

        action.Should().Throw<InvalidOperationException>();
    }
}
