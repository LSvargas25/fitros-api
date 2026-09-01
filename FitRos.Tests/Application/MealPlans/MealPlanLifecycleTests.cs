using FitRos.Application.Features.MealPlans.ActivateMealPlan;
using FitRos.Application.Features.MealPlans.ArchiveMealPlan;
using FitRos.Application.Features.MealPlans.GetActiveMealPlan;
using FitRos.Application.Features.MealPlans.GetClientMealPlans;
using FitRos.Application.Features.MealPlans.RenameMealPlan;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Nutrition;
using FitRos.Tests.Helpers;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.MealPlans;

public class MealPlanLifecycleTests
{
    private static FakeCurrentUser Admin(Guid gymId) => new(gymId)
    {
        UserId = Guid.NewGuid(),
        Role = UserRole.Admin,
        IsAuthenticated = true
    };

    [Fact]
    public async Task Activate_Then_GetActive_Returns_The_Plan()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);

        var client = ClientProfileMother.Create(gymId: gymId, coachId: Guid.NewGuid());
        context.ClientProfiles.Add(client);
        var plan = MealPlan.Create(client.Id, Guid.NewGuid(), gymId, "Plan");
        context.MealPlans.Add(plan);
        await context.SaveChangesAsync(CancellationToken.None);

        await new ActivateMealPlanHandler(context, user)
            .Handle(new ActivateMealPlanCommand(plan.Id), CancellationToken.None);

        context.MealPlans.Single(p => p.Id == plan.Id).Status.Should().Be(MealPlanStatus.Active);

        var active = await new GetActiveMealPlanHandler(context, user)
            .Handle(new GetActiveMealPlanQuery(client.Id), CancellationToken.None);

        active.Should().NotBeNull();
        active!.Id.Should().Be(plan.Id);
    }

    [Fact]
    public async Task Archive_Prevents_Rename()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);

        var plan = MealPlan.Create(Guid.NewGuid(), Guid.NewGuid(), gymId, "Plan");
        context.MealPlans.Add(plan);
        await context.SaveChangesAsync(CancellationToken.None);

        await new ArchiveMealPlanHandler(context, user)
            .Handle(new ArchiveMealPlanCommand(plan.Id), CancellationToken.None);

        context.MealPlans.Single(p => p.Id == plan.Id).Status.Should().Be(MealPlanStatus.Archived);

        await Assert.ThrowsAsync<DomainException>(() =>
            new RenameMealPlanHandler(context, user)
                .Handle(new RenameMealPlanCommand(plan.Id, "New Name"), CancellationToken.None));
    }

    [Fact]
    public async Task GetClientMealPlans_Returns_Plans_Newest_First()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);

        var client = ClientProfileMother.Create(gymId: gymId, coachId: Guid.NewGuid());
        context.ClientProfiles.Add(client);
        context.MealPlans.Add(MealPlan.Create(client.Id, Guid.NewGuid(), gymId, "Older"));
        await context.SaveChangesAsync(CancellationToken.None);
        await Task.Delay(5);
        context.MealPlans.Add(MealPlan.Create(client.Id, Guid.NewGuid(), gymId, "Newer"));
        await context.SaveChangesAsync(CancellationToken.None);

        var list = await new GetClientMealPlansHandler(context, user)
            .Handle(new GetClientMealPlansQuery(client.Id), CancellationToken.None);

        list.Should().HaveCount(2);
        list.First().Name.Should().Be("Newer");
    }
}
