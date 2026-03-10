using FitRos.Application.Features.WeeklyTrainingPlans.CreatePlan;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using FitRos.Tests.Helpers;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.WeeklyTrainingPlans;

public class CreatePlanTests
{
    [Fact]
    public async Task Should_Throw_When_Not_Authenticated()
    {
        var currentUser = new Mock<FitRos.Application.Abstractions.Security.ICurrentUser>();
        currentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var context = TestDbContextFactory.Create(currentUser.Object);
        var handler = new CreateWeeklyTrainingPlanHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(
                new CreateWeeklyTrainingPlanCommand(Guid.NewGuid(), "Plan"),
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
        var handler = new CreateWeeklyTrainingPlanHandler(context, fakeUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateWeeklyTrainingPlanCommand(Guid.NewGuid(), "Plan"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Not_Find_Client_From_Different_Gym()
    {
        // Tenant query filter hides profiles from other gyms, resulting in NotFoundException
        var gymId = Guid.NewGuid();
        var otherGymId = Guid.NewGuid();

        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        // Save a profile with a different gymId directly (tenant filter applies to reads, not writes)
        var clientProfile = ClientProfileMother.Create(gymId: otherGymId);
        context.ClientProfiles.Add(clientProfile);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateWeeklyTrainingPlanHandler(context, fakeUser);

        // The tenant filter hides the foreign-gym profile → NotFoundException
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateWeeklyTrainingPlanCommand(clientProfile.Id, "Plan"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Coach_Is_Not_Assigned_To_Client()
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
        var clientProfile = ClientProfileMother.Create(gymId: gymId, coachId: otherCoachId);
        context.ClientProfiles.Add(clientProfile);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateWeeklyTrainingPlanHandler(context, fakeUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new CreateWeeklyTrainingPlanCommand(clientProfile.Id, "Plan"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Create_Plan_When_Admin_Is_Authenticated()
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
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateWeeklyTrainingPlanHandler(context, fakeUser);

        var result = await handler.Handle(
            new CreateWeeklyTrainingPlanCommand(clientProfile.Id, "Strength Plan"),
            CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        context.WeeklyTrainingPlans.Should().ContainSingle(p => p.Id == result.Id);
    }

    [Fact]
    public async Task Should_Create_Plan_When_Coach_Is_Assigned()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = coachId,
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var clientProfile = ClientProfileMother.Create(gymId: gymId, coachId: coachId);
        context.ClientProfiles.Add(clientProfile);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateWeeklyTrainingPlanHandler(context, fakeUser);

        var result = await handler.Handle(
            new CreateWeeklyTrainingPlanCommand(clientProfile.Id, "Hypertrophy Plan"),
            CancellationToken.None);

        result.Id.Should().NotBeEmpty();
    }
}
