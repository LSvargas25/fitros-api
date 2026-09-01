using System;
using System.Linq;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using Xunit;

namespace FitRos.Tests.Domain.WorkoutSessions;

public class WorkoutSessionTests
{
    // Regression test for a removed WorkoutSession.Create(gymId, coachId,
    // clientId, notes, duration) overload that forwarded into this 6-arg
    // factory with mismatched parameter meanings (coachId -> userId,
    // clientId -> routineId, notes -> routineName, duration -> routineVersion).
    // It was never called by any production code or test, but was a trap for
    // the next caller - removed rather than fixed. This test locks down that
    // the one remaining Create correctly assigns each argument to its own
    // property.
    [Fact]
    public void Create_Should_Assign_Each_Argument_To_Its_Own_Property()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var routineId = Guid.NewGuid();
        var scheduledDate = DateTime.UtcNow.Date;

        var session = WorkoutSession.Create(gymId, userId, routineId, "Piernas", 3, scheduledDate);

        Assert.Equal(gymId, session.GymId);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(routineId, session.RoutineId);
        Assert.Equal("Piernas", session.RoutineNameSnapshot);
        Assert.Equal(3, session.RoutineVersion);
        Assert.Equal(scheduledDate, session.ScheduledDate);
        Assert.Equal(WorkoutSessionStatus.Scheduled, session.Status);
        Assert.Empty(session.Sets);
    }

    [Fact]
    public void Start_Should_Set_Status_To_InProgress()
    {
        var session = WorkoutSession.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Piernas", 1, DateTime.UtcNow.Date);

        session.Start();

        Assert.Equal(WorkoutSessionStatus.InProgress, session.Status);
    }

    [Fact]
    public void AddSet_Should_Throw_When_Not_InProgress()
    {
        var session = WorkoutSession.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Piernas", 1, DateTime.UtcNow.Date);

        Assert.Throws<InvalidOperationException>(() =>
            session.AddSet(Guid.NewGuid(), 1, 10, 40));
    }

    [Fact]
    public void RemoveSet_Should_Remove_Existing_Set()
    {
        var session = WorkoutSession.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Piernas", 1, DateTime.UtcNow.Date);
        session.Start();
        session.AddSet(Guid.NewGuid(), 1, 10, 40);
        var setId = session.Sets.First().Id;

        session.RemoveSet(setId);

        Assert.Empty(session.Sets);
    }

    [Fact]
    public void RemoveSet_Should_Throw_When_Set_Not_Found()
    {
        var session = WorkoutSession.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Piernas", 1, DateTime.UtcNow.Date);
        session.Start();

        Assert.Throws<InvalidOperationException>(() => session.RemoveSet(Guid.NewGuid()));
    }
}
