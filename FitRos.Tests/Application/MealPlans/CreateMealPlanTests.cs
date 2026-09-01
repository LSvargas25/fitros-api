using FitRos.Application.Features.MealPlans.CreateMealPlan;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using FitRos.Domain.Entities.Enums;
using FitRos.Tests.Helpers;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.MealPlans;

public class CreateMealPlanTests
{
    [Fact]
    public async Task Should_Throw_When_Not_Authenticated()
    {
        var currentUser = new Mock<FitRos.Application.Abstractions.Security.ICurrentUser>();
        currentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var context = TestDbContextFactory.Create(currentUser.Object);
        var handler = new CreateMealPlanHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new CreateMealPlanCommand(Guid.NewGuid(), "Cut"), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Client_Not_Found()
    {
        var user = new FakeCurrentUser(Guid.NewGuid())
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(user);
        var handler = new CreateMealPlanHandler(context, user);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CreateMealPlanCommand(Guid.NewGuid(), "Cut"), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Not_Find_Client_From_Different_Gym()
    {
        var gymId = Guid.NewGuid();
        var otherGymId = Guid.NewGuid();

        var user = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(user);
        var client = ClientProfileMother.Create(gymId: otherGymId);
        context.ClientProfiles.Add(client);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateMealPlanHandler(context, user);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CreateMealPlanCommand(client.Id, "Cut"), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Coach_Is_Not_Assigned_To_Client()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var user = new FakeCurrentUser(gymId)
        {
            UserId = coachId,
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(user);
        var client = ClientProfileMother.Create(gymId: gymId, coachId: Guid.NewGuid());
        context.ClientProfiles.Add(client);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateMealPlanHandler(context, user);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new CreateMealPlanCommand(client.Id, "Cut"), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Create_Draft_Plan_For_Admin()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var user = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(user);
        var client = ClientProfileMother.Create(gymId: gymId, coachId: coachId);
        context.ClientProfiles.Add(client);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateMealPlanHandler(context, user);

        var result = await handler.Handle(new CreateMealPlanCommand(client.Id, "Cutting Plan"), CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        var plan = context.MealPlans.Single(p => p.Id == result.Id);
        plan.Status.Should().Be(MealPlanStatus.Draft);
        plan.ClientProfileId.Should().Be(client.Id);
        plan.CoachId.Should().Be(coachId);
        plan.GymId.Should().Be(gymId);
    }

    [Fact]
    public async Task Should_Create_Plan_When_Coach_Is_Assigned()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var user = new FakeCurrentUser(gymId)
        {
            UserId = coachId,
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(user);
        var client = ClientProfileMother.Create(gymId: gymId, coachId: coachId);
        context.ClientProfiles.Add(client);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateMealPlanHandler(context, user);

        var result = await handler.Handle(new CreateMealPlanCommand(client.Id, "Bulk"), CancellationToken.None);

        result.Id.Should().NotBeEmpty();
    }
}
