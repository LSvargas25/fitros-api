using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FluentAssertions;

namespace FitRos.Tests.Domain.Training;

public class WorkoutSessionTests
{
    private WorkoutSession CreateValidSession()
    {
        return WorkoutSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Push Day",
            1,
            DateTime.UtcNow);
    }

    [Fact]
    public void Should_Start_Session_From_Scheduled()
    {
        var session = CreateValidSession();

        session.Start();

        session.Status.Should().Be(WorkoutSessionStatus.InProgress);
    }

    [Fact]
    public void Should_Not_Complete_Without_Sets()
    {
        var session = CreateValidSession();
        session.Start();

        var action = () => session.Complete();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Should_Complete_With_At_Least_One_Set()
    {
        var session = CreateValidSession();
        session.Start();

        session.AddSet(Guid.NewGuid(), 1, 10, 50);

        session.Complete();

        session.Status.Should().Be(WorkoutSessionStatus.Completed);
    }

    [Fact]
    public void Should_Not_Add_Set_If_Not_InProgress()
    {
        var session = CreateValidSession();

        var action = () =>
            session.AddSet(Guid.NewGuid(), 1, 10, 50);

        action.Should().Throw<InvalidOperationException>();
    }
}
