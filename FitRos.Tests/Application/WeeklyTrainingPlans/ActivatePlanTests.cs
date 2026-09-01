using FitRos.Application.Features.WeeklyTrainingPlans.ActivatePlan;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.WeeklyTraining;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.WeeklyTrainingPlans;

public class ActivatePlanTests
{
    [Fact]
    public async Task Should_Throw_When_Not_Authenticated()
    {
        var currentUser = new Mock<FitRos.Application.Abstractions.Security.ICurrentUser>();
        currentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var context = TestDbContextFactory.Create(currentUser.Object);
        var handler = new ActivatePlanHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(
                new ActivatePlanCommand(Guid.NewGuid()),
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
        var handler = new ActivatePlanHandler(context, fakeUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new ActivatePlanCommand(Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Activating_A_Plan_Archives_The_Client_Other_Active_Plans()
    {
        var gymId = Guid.NewGuid();
        var clientProfileId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = coachId,
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var currentlyActive = WeeklyTrainingPlan.Create(clientProfileId, coachId, gymId, "Plan A");
        currentlyActive.Activate();

        var toActivate = WeeklyTrainingPlan.Create(clientProfileId, coachId, gymId, "Plan B");

        context.WeeklyTrainingPlans.AddRange(currentlyActive, toActivate);
        await context.SaveChangesAsync();

        var handler = new ActivatePlanHandler(context, fakeUser);

        await handler.Handle(new ActivatePlanCommand(toActivate.Id), CancellationToken.None);

        var reloadedActive = await context.WeeklyTrainingPlans.FirstAsync(x => x.Id == currentlyActive.Id);
        var reloadedTarget = await context.WeeklyTrainingPlans.FirstAsync(x => x.Id == toActivate.Id);

        reloadedActive.Status.Should().Be(TrainingPlanStatus.Archived);
        reloadedTarget.Status.Should().Be(TrainingPlanStatus.Active);
    }
}
