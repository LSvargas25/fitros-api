using FitRos.Application.Features.WeeklyTrainingPlans.AssignRoutineToDay;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.WeeklyTraining;
using FitRos.Domain.Enums;
using FitRos.Tests.Helpers;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.WeeklyTrainingPlans;

public class AssignRoutineToDayTests
{
    [Fact]
    public async Task Should_Throw_When_Not_Authenticated()
    {
        var currentUser = new Mock<FitRos.Application.Abstractions.Security.ICurrentUser>();
        currentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var context = TestDbContextFactory.Create(currentUser.Object);
        var handler = new AssignRoutineToDayHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(
                new AssignRoutineToDayCommand(Guid.NewGuid(), DayOfWeek.Monday, Guid.NewGuid(), null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Plan_Not_Found()
    {
        var gymId = Guid.NewGuid();
        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var handler = new AssignRoutineToDayHandler(context, fakeUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new AssignRoutineToDayCommand(Guid.NewGuid(), DayOfWeek.Monday, Guid.NewGuid(), null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Coach_Is_Not_Assigned()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var otherCoachId = Guid.NewGuid();

        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = coachId,
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var plan = WeeklyTrainingPlan.Create(Guid.NewGuid(), otherCoachId, gymId, "Plan");
        context.WeeklyTrainingPlans.Add(plan);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AssignRoutineToDayHandler(context, fakeUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new AssignRoutineToDayCommand(plan.Id, DayOfWeek.Monday, Guid.NewGuid(), null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Routine_Not_Found()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = coachId,
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var plan = WeeklyTrainingPlan.Create(Guid.NewGuid(), coachId, gymId, "Plan");
        context.WeeklyTrainingPlans.Add(plan);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AssignRoutineToDayHandler(context, fakeUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new AssignRoutineToDayCommand(plan.Id, DayOfWeek.Monday, Guid.NewGuid(), null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Assign_Routine_When_Valid()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var container = TestDbContextFactory.CreateContainer(UserRole.Admin);
        container.CurrentUser.GymId = gymId;
        container.CurrentUser.UserId = coachId;
        var context = container.Context;

        var plan = WeeklyTrainingPlan.Create(Guid.NewGuid(), coachId, gymId, "Plan");
        context.WeeklyTrainingPlans.Add(plan);

        var routine = FitRos.Domain.Entities.Training.WorkoutRoutine.Create(
            gymId, "Push Day", "Upper body push routine");
        context.WorkoutRoutines.Add(routine);

        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AssignRoutineToDayHandler(context, container.CurrentUser);

        await handler.Handle(
            new AssignRoutineToDayCommand(plan.Id, DayOfWeek.Monday, routine.Id, "Focus on chest"),
            CancellationToken.None);

        var updatedPlan = context.WeeklyTrainingPlans
            .Include(p => p.Days)
            .First(p => p.Id == plan.Id);

        updatedPlan.Days.Should().ContainSingle(d => d.Day == DayOfWeek.Monday);
        updatedPlan.Days.First(d => d.Day == DayOfWeek.Monday).WorkoutRoutineId.Should().Be(routine.Id);
    }
}
