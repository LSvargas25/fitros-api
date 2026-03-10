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
    public async Task Should_Activate_Plan_When_Valid()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var plan = WeeklyTrainingPlan.Create(Guid.NewGuid(), coachId, gymId, "Plan");
        context.WeeklyTrainingPlans.Add(plan);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ActivatePlanHandler(context, fakeUser);

        await handler.Handle(new ActivatePlanCommand(plan.Id), CancellationToken.None);

        var updated = await context.WeeklyTrainingPlans.SingleAsync(p => p.Id == plan.Id);
        updated.Status.Should().Be(TrainingPlanStatus.Active);
    }
}
