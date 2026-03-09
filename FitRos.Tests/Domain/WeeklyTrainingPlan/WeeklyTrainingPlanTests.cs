using FitRos.Domain.Common;
using FitRos.Domain.Entities.WeeklyTraining;
using Xunit;

namespace FitRos.Tests.Domain.WeeklyTrainingPlans;

public class WeeklyTrainingPlanTests
{
    private static WeeklyTrainingPlan CreateValidPlan(Guid? gymId = null, Guid? coachId = null, Guid? clientProfileId = null)
        => WeeklyTrainingPlan.Create(
            clientProfileId ?? Guid.NewGuid(),
            coachId ?? Guid.NewGuid(),
            gymId ?? Guid.NewGuid(),
            "Strength Plan");

    [Fact]
    public void Create_Should_Return_Plan_With_Draft_Status()
    {
        var plan = CreateValidPlan();

        Assert.Equal(FitRos.Domain.Entities.Enums.TrainingPlanStatus.Draft, plan.Status);
    }

    [Fact]
    public void Create_Should_Set_CreatedAt_To_UtcNow()
    {
        var before = DateTime.UtcNow;
        var plan = CreateValidPlan();

        Assert.True(plan.CreatedAt >= before);
    }

    [Fact]
    public void Create_Should_Throw_When_Name_Is_Empty()
    {
        Assert.Throws<DomainException>(() =>
            WeeklyTrainingPlan.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), string.Empty));
    }

    [Fact]
    public void Create_Should_Throw_When_ClientProfileId_Is_Empty()
    {
        Assert.Throws<DomainException>(() =>
            WeeklyTrainingPlan.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Plan"));
    }

    [Fact]
    public void Create_Should_Throw_When_CoachId_Is_Empty()
    {
        Assert.Throws<DomainException>(() =>
            WeeklyTrainingPlan.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "Plan"));
    }

    [Fact]
    public void AssignRoutineToDay_Should_Add_Day()
    {
        var plan = CreateValidPlan();
        var routineId = Guid.NewGuid();

        plan.AssignRoutineToDay(DayOfWeek.Monday, routineId);

        Assert.Single(plan.Days);
        Assert.Equal(DayOfWeek.Monday, plan.Days.First().Day);
        Assert.Equal(routineId, plan.Days.First().WorkoutRoutineId);
    }

    [Fact]
    public void AssignRoutineToDay_Should_Update_Existing_Day()
    {
        var plan = CreateValidPlan();
        var originalRoutineId = Guid.NewGuid();
        var newRoutineId = Guid.NewGuid();

        plan.AssignRoutineToDay(DayOfWeek.Monday, originalRoutineId);
        plan.AssignRoutineToDay(DayOfWeek.Monday, newRoutineId, "Updated notes");

        Assert.Single(plan.Days);
        Assert.Equal(newRoutineId, plan.Days.First().WorkoutRoutineId);
        Assert.Equal("Updated notes", plan.Days.First().Notes);
    }

    [Fact]
    public void AssignRoutineToDay_Should_Throw_When_RoutineId_Is_Empty()
    {
        var plan = CreateValidPlan();

        Assert.Throws<DomainException>(() =>
            plan.AssignRoutineToDay(DayOfWeek.Monday, Guid.Empty));
    }

    [Fact]
    public void AssignRoutineToDay_Should_Throw_When_Plan_Is_Archived()
    {
        var plan = CreateValidPlan();
        plan.Archive();

        Assert.Throws<DomainException>(() =>
            plan.AssignRoutineToDay(DayOfWeek.Monday, Guid.NewGuid()));
    }

    [Fact]
    public void RemoveRoutineFromDay_Should_Remove_Day()
    {
        var plan = CreateValidPlan();
        plan.AssignRoutineToDay(DayOfWeek.Monday, Guid.NewGuid());

        plan.RemoveRoutineFromDay(DayOfWeek.Monday);

        Assert.Empty(plan.Days);
    }

    [Fact]
    public void RemoveRoutineFromDay_Should_Throw_When_Day_Not_Assigned()
    {
        var plan = CreateValidPlan();

        Assert.Throws<DomainException>(() =>
            plan.RemoveRoutineFromDay(DayOfWeek.Monday));
    }

    [Fact]
    public void RemoveRoutineFromDay_Should_Throw_When_Plan_Is_Archived()
    {
        var plan = CreateValidPlan();
        plan.AssignRoutineToDay(DayOfWeek.Monday, Guid.NewGuid());
        plan.Archive();

        Assert.Throws<DomainException>(() =>
            plan.RemoveRoutineFromDay(DayOfWeek.Monday));
    }

    [Fact]
    public void Activate_Should_Set_Status_To_Active()
    {
        var plan = CreateValidPlan();

        plan.Activate();

        Assert.Equal(FitRos.Domain.Entities.Enums.TrainingPlanStatus.Active, plan.Status);
    }

    [Fact]
    public void Activate_Should_Throw_When_Already_Active()
    {
        var plan = CreateValidPlan();
        plan.Activate();

        Assert.Throws<DomainException>(() => plan.Activate());
    }

    [Fact]
    public void Activate_Should_Throw_When_Archived()
    {
        var plan = CreateValidPlan();
        plan.Archive();

        Assert.Throws<DomainException>(() => plan.Activate());
    }

    [Fact]
    public void Archive_Should_Set_Status_To_Archived()
    {
        var plan = CreateValidPlan();

        plan.Archive();

        Assert.Equal(FitRos.Domain.Entities.Enums.TrainingPlanStatus.Archived, plan.Status);
    }

    [Fact]
    public void Archive_Should_Throw_When_Already_Archived()
    {
        var plan = CreateValidPlan();
        plan.Archive();

        Assert.Throws<DomainException>(() => plan.Archive());
    }

    [Fact]
    public void Deactivate_Should_Revert_To_Draft()
    {
        var plan = CreateValidPlan();
        plan.Activate();

        plan.Deactivate();

        Assert.Equal(FitRos.Domain.Entities.Enums.TrainingPlanStatus.Draft, plan.Status);
    }

    [Fact]
    public void Deactivate_Should_Throw_When_Not_Active()
    {
        var plan = CreateValidPlan();

        Assert.Throws<DomainException>(() => plan.Deactivate());
    }

    [Fact]
    public void Rename_Should_Update_Name()
    {
        var plan = CreateValidPlan();

        plan.Rename("New Name");

        Assert.Equal("New Name", plan.Name);
    }

    [Fact]
    public void Rename_Should_Throw_When_Name_Is_Empty()
    {
        var plan = CreateValidPlan();

        Assert.Throws<DomainException>(() => plan.Rename(string.Empty));
    }

    [Fact]
    public void Rename_Should_Throw_When_Archived()
    {
        var plan = CreateValidPlan();
        plan.Archive();

        Assert.Throws<DomainException>(() => plan.Rename("New Name"));
    }
}
