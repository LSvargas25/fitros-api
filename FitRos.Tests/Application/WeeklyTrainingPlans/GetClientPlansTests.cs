using FitRos.Application.Features.WeeklyTrainingPlans.GetClientPlans;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.WeeklyTraining;
using FitRos.Domain.Enums;
using FitRos.Tests.Helpers;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.WeeklyTrainingPlans;

public class GetClientPlansTests
{
    [Fact]
    public async Task Should_Throw_When_Not_Authenticated()
    {
        var currentUser = new Mock<FitRos.Application.Abstractions.Security.ICurrentUser>();
        currentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var context = TestDbContextFactory.Create(currentUser.Object);
        var handler = new GetClientPlansHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(
                new GetClientPlansQuery(Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Client_Not_Found()
    {
        var gymId = Guid.NewGuid();
        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var handler = new GetClientPlansHandler(context, fakeUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new GetClientPlansQuery(Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Return_Plans_For_Client()
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

        var clientProfile = ClientProfileMother.Create(gymId: gymId, coachId: coachId);
        context.ClientProfiles.Add(clientProfile);

        var plan1 = WeeklyTrainingPlan.Create(clientProfile.Id, coachId, gymId, "Plan A");
        var plan2 = WeeklyTrainingPlan.Create(clientProfile.Id, coachId, gymId, "Plan B");
        context.WeeklyTrainingPlans.Add(plan1);
        context.WeeklyTrainingPlans.Add(plan2);

        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClientPlansHandler(context, fakeUser);

        var result = await handler.Handle(
            new GetClientPlansQuery(clientProfile.Id),
            CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_Return_Empty_When_No_Plans_Exist()
    {
        var gymId = Guid.NewGuid();

        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var clientProfile = ClientProfileMother.Create(gymId: gymId);
        context.ClientProfiles.Add(clientProfile);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClientPlansHandler(context, fakeUser);

        var result = await handler.Handle(
            new GetClientPlansQuery(clientProfile.Id),
            CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Return_Correct_DayCount()
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
        var clientProfile = ClientProfileMother.Create(gymId: gymId, coachId: coachId);
        context.ClientProfiles.Add(clientProfile);

        var plan = WeeklyTrainingPlan.Create(clientProfile.Id, coachId, gymId, "Full Week");
        plan.AssignRoutineToDay(DayOfWeek.Monday, Guid.NewGuid());
        plan.AssignRoutineToDay(DayOfWeek.Wednesday, Guid.NewGuid());
        plan.AssignRoutineToDay(DayOfWeek.Friday, Guid.NewGuid());
        context.WeeklyTrainingPlans.Add(plan);

        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetClientPlansHandler(context, fakeUser);

        var result = await handler.Handle(
            new GetClientPlansQuery(clientProfile.Id),
            CancellationToken.None);

        result.Should().ContainSingle();
        result[0].DayCount.Should().Be(3);
    }
}
