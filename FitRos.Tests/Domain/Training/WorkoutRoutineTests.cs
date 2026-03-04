 
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
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest and triceps");

        routine.Status.Should().Be(RoutineStatus.Draft);
        routine.Version.Should().Be(1);
        routine.Name.Should().Be("Push Day");
        routine.NormalizedName.Should().Be("push day");
    }

    [Fact]
    public void Should_Not_Create_Routine_With_Empty_Name()
    {
        var act = () => WorkoutRoutine.Create(Guid.NewGuid(), "", "desc");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_Not_Allow_AddExercise_When_Published()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);
        routine.Publish();

        var act = () => routine.AddExercise(Guid.NewGuid(), 2, 3, 12, 60);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Routine can only be modified in Draft state.");
    }

    [Fact]
    public void Should_Not_Allow_UpdateDetails_When_Published()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);
        routine.Publish();

        var act = () => routine.UpdateDetails("New", "Desc");

        act.Should().Throw<DomainException>()
           .WithMessage("Routine can only be modified in Draft state.");
    }

    [Fact]
    public void Should_Not_Publish_Routine_Without_Exercises()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest");

        var act = () => routine.Publish();

        act.Should().Throw<DomainException>()
           .WithMessage("Cannot publish a routine without exercises.");
    }

    [Fact]
    public void Should_Not_Publish_When_Not_Draft()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);
        routine.Publish();

        var act = () => routine.Publish();

        act.Should().Throw<DomainException>()
           .WithMessage("Only draft routines can be published.");
    }

    [Fact]
    public void Should_Not_Archive_When_Not_Published()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest");

        var act = () => routine.Archive();

        act.Should().Throw<DomainException>()
           .WithMessage("Only published routines can be archived.");
    }

    [Fact]
    public void Should_Create_New_Version_From_Published_Routine_With_Incremented_Version_And_Draft_Status()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest");
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
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest");

        var act = () => routine.CreateNewVersion();

        act.Should().Throw<DomainException>()
           .WithMessage("Only published routines can be versioned.");
    }

    [Fact]
    public void RemoveExercise_Should_Throw_When_Routine_Is_Published()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Test Routine", "Desc");

        var exerciseId = Guid.NewGuid();
        routine.AddExercise(exerciseId, 1, 3, 10, 60);

        routine.Publish();

        var act = () => routine.RemoveExercise(exerciseId);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Routine can only be modified in Draft state.");
    }

    [Fact]
    public void RemoveExercise_Should_Throw_When_Exercise_Not_Found()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Test Routine", "Desc");

        var exerciseId = Guid.NewGuid();
        routine.AddExercise(exerciseId, 1, 3, 10, 60);

        var act = () => routine.RemoveExercise(Guid.NewGuid());

        act.Should().Throw<DomainException>()
            .WithMessage("Exercise not found in routine.");
    }

    [Fact]
    public void RemoveExercise_Should_Remove_Exercise_And_Reorder()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Test Routine", "Desc");

        var exercise1 = Guid.NewGuid();
        var exercise2 = Guid.NewGuid();
        var exercise3 = Guid.NewGuid();

        routine.AddExercise(exercise1, 1, 3, 10, 60);
        routine.AddExercise(exercise2, 2, 3, 10, 60);
        routine.AddExercise(exercise3, 3, 3, 10, 60);

        routine.RemoveExercise(exercise2);

        routine.Exercises.Count.Should().Be(2);

        routine.Exercises.Should()
            .ContainSingle(e => e.ExerciseId == exercise1 && e.Order == 1);

        routine.Exercises.Should()
            .ContainSingle(e => e.ExerciseId == exercise3 && e.Order == 2);
    }

    [Fact]
    public void Should_Set_RoutineGroupId_To_Id_When_Creating_Routine()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest");

        routine.RoutineGroupId.Should().Be(routine.Id);
    }

    [Fact]
    public void Should_Create_New_Version_Keeping_RoutineGroupId_And_Incrementing_Version()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);
        routine.Publish();

        var originalId = routine.Id;
        var originalGroupId = routine.RoutineGroupId;
        var originalVersion = routine.Version;

        var newRoutine = routine.CreateNewVersion();

        newRoutine.Id.Should().NotBe(originalId);
        newRoutine.RoutineGroupId.Should().Be(originalGroupId);

        newRoutine.Status.Should().Be(RoutineStatus.Draft);
        newRoutine.Version.Should().Be(originalVersion + 1);

        newRoutine.Name.Should().Be(routine.Name);
        newRoutine.NormalizedName.Should().Be(routine.NormalizedName);
        newRoutine.Description.Should().Be(routine.Description);
    }

    [Fact]
    public void Should_Copy_All_Exercises_With_Same_Order_And_Suggested_Values_When_Creating_New_Version()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Leg Day", "Quads");
        var ex1 = Guid.NewGuid();
        var ex2 = Guid.NewGuid();
        var ex3 = Guid.NewGuid();

        routine.AddExercise(ex1, 1, 4, 8, 120);
        routine.AddExercise(ex2, 2, 3, 12, 90);
        routine.AddExercise(ex3, 3, 5, 6, 150);

        routine.Publish();

        var newRoutine = routine.CreateNewVersion();

        newRoutine.Exercises.Should().HaveCount(3);

        newRoutine.Exercises.Should().ContainSingle(e =>
            e.ExerciseId == ex1 &&
            e.Order == 1 &&
            e.SuggestedSets == 4 &&
            e.SuggestedReps == 8 &&
            e.SuggestedRestSeconds == 120);

        newRoutine.Exercises.Should().ContainSingle(e =>
            e.ExerciseId == ex2 &&
            e.Order == 2 &&
            e.SuggestedSets == 3 &&
            e.SuggestedReps == 12 &&
            e.SuggestedRestSeconds == 90);

        newRoutine.Exercises.Should().ContainSingle(e =>
            e.ExerciseId == ex3 &&
            e.Order == 3 &&
            e.SuggestedSets == 5 &&
            e.SuggestedReps == 6 &&
            e.SuggestedRestSeconds == 150);
    }

    [Fact]
    public void Should_Not_Create_New_Version_If_Routine_Is_Draft()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest");

        var act = () => routine.CreateNewVersion();

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Only published routines can be versioned.");
    }

    [Fact]
    public void Should_Not_Create_New_Version_If_Routine_Is_Archived()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);
        routine.Publish();
        routine.Archive();

        var act = () => routine.CreateNewVersion();

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Only published routines can be versioned.");
    }

    [Fact]
    public void Should_Update_Details_In_Draft_And_Normalize_Name_And_Keep_Version()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest");
        var v0 = routine.Version;

        routine.UpdateDetails("  Upper Push  ", "  Chest and shoulders  ");

        routine.Name.Should().Be("Upper Push");
        routine.NormalizedName.Should().Be("upper push");
        routine.Description.Should().Be("Chest and shoulders");
        routine.Version.Should().Be(v0);
    }

    [Fact]
    public void Should_Archive_Only_When_Published_And_Keep_Version()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);
        routine.Publish();

        var v0 = routine.Version;

        routine.Archive();

        routine.Status.Should().Be(RoutineStatus.Archived);
        routine.Version.Should().Be(v0);
    }

    [Fact]
    public void Should_Publish_From_Draft_With_Exercises_And_Keep_Version()
    {
        var routine = WorkoutRoutine.Create(Guid.NewGuid(), "Push Day", "Chest");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 90);

        var v0 = routine.Version;

        routine.Publish();

        routine.Status.Should().Be(RoutineStatus.Published);
        routine.Version.Should().Be(v0);
    }
}
