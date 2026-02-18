using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FitRos.Domain.Common;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Domain.Training;

public class WorkoutRoutineTests
{
    [Fact]
    public void Should_Create_Routine_In_Draft_State_With_Version_1()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest and triceps");

        routine.Status.Should().Be(RoutineStatus.Draft);
        routine.Version.Should().Be(1);
        routine.Name.Should().Be("Push Day");
        routine.NormalizedName.Should().Be("push day");
    }

    [Fact]
    public void Should_Not_Create_Routine_With_Empty_Name()
    {
        var act = () => WorkoutRoutine.Create("", "desc");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_Add_Exercise_In_Draft_State_And_Increment_Version()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");
        var v0 = routine.Version;

        routine.AddExercise(Guid.NewGuid(), order: 1, suggestedSets: 4, suggestedReps: 10, suggestedRestSeconds: 90);

        routine.Exercises.Should().HaveCount(1);
        routine.Version.Should().Be(v0 + 1);
    }

    [Fact]
    public void Should_Not_Allow_AddExercise_When_Published()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);
        routine.Publish();

        var act = () => routine.AddExercise(Guid.NewGuid(), 2, 3, 12, 60);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Routine can only be modified in Draft state.");
    }

    [Fact]
    public void Should_Update_Details_In_Draft_And_Increment_Version_And_Normalize_Name()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");
        var v0 = routine.Version;

        routine.UpdateDetails("  Upper Push  ", "  Chest and shoulders  ");

        routine.Name.Should().Be("Upper Push");
        routine.NormalizedName.Should().Be("upper push");
        routine.Description.Should().Be("Chest and shoulders");
        routine.Version.Should().Be(v0 + 1);
    }

    [Fact]
    public void Should_Not_Allow_UpdateDetails_When_Published()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);
        routine.Publish();

        var act = () => routine.UpdateDetails("New", "Desc");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Routine can only be modified in Draft state.");
    }

    [Fact]
    public void Should_Not_Publish_Routine_Without_Exercises()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");

        var act = () => routine.Publish();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Cannot publish a routine without exercises.");
    }

    [Fact]
    public void Should_Publish_From_Draft_With_Exercises_And_Increment_Version()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);

        var v0 = routine.Version;

        routine.Publish();

        routine.Status.Should().Be(RoutineStatus.Published);
        routine.Version.Should().Be(v0 + 1);
    }

    [Fact]
    public void Should_Not_Publish_When_Not_Draft()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);
        routine.Publish();

        var act = () => routine.Publish();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Only draft routines can be published.");
    }

    [Fact]
    public void Should_Archive_Only_When_Published_And_Increment_Version()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);
        routine.Publish();

        var v0 = routine.Version;

        routine.Archive();

        routine.Status.Should().Be(RoutineStatus.Archived);
        routine.Version.Should().Be(v0 + 1);
    }

    [Fact]
    public void Should_Not_Archive_When_Not_Published()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");

        var act = () => routine.Archive();

        act.Should().Throw<DomainException>()
           .WithMessage("Only published routines can be archived.");
    }

    [Fact]
    public void Should_Create_New_Version_From_Published_Routine_With_Incremented_Version_And_Draft_Status()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);
        routine.Publish();

        var currentVersion = routine.Version;
        var newRoutine = routine.CreateNewVersion();

        newRoutine.Id.Should().NotBe(routine.Id);
        newRoutine.Status.Should().Be(RoutineStatus.Draft);
        newRoutine.Version.Should().Be(currentVersion + 1);

        newRoutine.Name.Should().Be(routine.Name);
        newRoutine.Description.Should().Be(routine.Description);

        newRoutine.Exercises.Should().HaveCount(routine.Exercises.Count);
    }

    [Fact]
    public void Should_Not_Create_New_Version_If_Not_Published()
    {
        var routine = WorkoutRoutine.Create("Push Day", "Chest");

        var act = () => routine.CreateNewVersion();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Only published routines can be versioned.");
    }
}
