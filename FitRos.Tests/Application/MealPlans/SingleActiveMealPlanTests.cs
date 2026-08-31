using FitRos.Application.Features.MealPlans.ActivateMealPlan;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Nutrition;
using FitRos.Domain.Enums;
using FitRos.Tests.Helpers;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.MealPlans;

// Part 2 — activating a meal plan supersedes the client's previously Active one.
public class SingleActiveMealPlanTests
{
    private static FakeCurrentUser Admin(Guid gymId) => new(gymId)
    {
        UserId = Guid.NewGuid(),
        Role = UserRole.Admin,
        IsAuthenticated = true
    };

    [Fact]
    public async Task Activating_A_Plan_Archives_The_Previously_Active_Plan_For_The_Same_Client()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var ctx = TestDbContextFactory.Create(user);

        var client = ClientProfileMother.Create(gymId: gymId, coachId: Guid.NewGuid());
        ctx.ClientProfiles.Add(client);

        var first = MealPlan.Create(client.Id, Guid.NewGuid(), gymId, "Cut");
        var second = MealPlan.Create(client.Id, Guid.NewGuid(), gymId, "Maintain");
        ctx.MealPlans.AddRange(first, second);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new ActivateMealPlanHandler(ctx, user);
        await handler.Handle(new ActivateMealPlanCommand(first.Id), CancellationToken.None);
        await handler.Handle(new ActivateMealPlanCommand(second.Id), CancellationToken.None);

        ctx.MealPlans.Single(p => p.Id == second.Id).Status.Should().Be(MealPlanStatus.Active);
        ctx.MealPlans.Single(p => p.Id == first.Id).Status
            .Should().Be(MealPlanStatus.Archived, "the previous active plan is superseded, not deleted");
        ctx.MealPlans.Where(p => p.ClientProfileId == client.Id).Should().HaveCount(2);
    }

    [Fact]
    public async Task Activating_Does_Not_Touch_Another_Clients_Active_Plan()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var ctx = TestDbContextFactory.Create(user);

        var clientA = ClientProfileMother.Create(gymId: gymId, coachId: Guid.NewGuid());
        var clientB = ClientProfileMother.Create(gymId: gymId, coachId: Guid.NewGuid());
        ctx.ClientProfiles.AddRange(clientA, clientB);

        var planA = MealPlan.Create(clientA.Id, Guid.NewGuid(), gymId, "A");
        var planB = MealPlan.Create(clientB.Id, Guid.NewGuid(), gymId, "B");
        ctx.MealPlans.AddRange(planA, planB);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new ActivateMealPlanHandler(ctx, user);
        await handler.Handle(new ActivateMealPlanCommand(planA.Id), CancellationToken.None);
        await handler.Handle(new ActivateMealPlanCommand(planB.Id), CancellationToken.None);

        ctx.MealPlans.Single(p => p.Id == planA.Id).Status.Should().Be(MealPlanStatus.Active);
    }
}
