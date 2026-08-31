using FitRos.Application.Features.WeeklyTrainingPlans.ActivatePlan;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.WeeklyTraining;
using FitRos.Domain.Enums;
using FitRos.Tests.Helpers;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.WeeklyTrainingPlans;

// Part 2 — activating a training plan supersedes the client's previously Active one.
public class SingleActiveTrainingPlanTests
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

        var first = WeeklyTrainingPlan.Create(client.Id, Guid.NewGuid(), gymId, "Block 1");
        var second = WeeklyTrainingPlan.Create(client.Id, Guid.NewGuid(), gymId, "Block 2");
        ctx.WeeklyTrainingPlans.AddRange(first, second);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new ActivatePlanHandler(ctx, user);
        await handler.Handle(new ActivatePlanCommand(first.Id), CancellationToken.None);
        await handler.Handle(new ActivatePlanCommand(second.Id), CancellationToken.None);

        ctx.WeeklyTrainingPlans.Single(p => p.Id == second.Id).Status
            .Should().Be(TrainingPlanStatus.Active);
        ctx.WeeklyTrainingPlans.Single(p => p.Id == first.Id).Status
            .Should().Be(TrainingPlanStatus.Archived, "the previous active plan is superseded, not deleted");
    }

    [Fact]
    public async Task Superseded_Plan_Stays_In_The_Clients_History()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var ctx = TestDbContextFactory.Create(user);

        var client = ClientProfileMother.Create(gymId: gymId, coachId: Guid.NewGuid());
        ctx.ClientProfiles.Add(client);
        var first = WeeklyTrainingPlan.Create(client.Id, Guid.NewGuid(), gymId, "Block 1");
        var second = WeeklyTrainingPlan.Create(client.Id, Guid.NewGuid(), gymId, "Block 2");
        ctx.WeeklyTrainingPlans.AddRange(first, second);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new ActivatePlanHandler(ctx, user);
        await handler.Handle(new ActivatePlanCommand(first.Id), CancellationToken.None);
        await handler.Handle(new ActivatePlanCommand(second.Id), CancellationToken.None);

        ctx.WeeklyTrainingPlans.Where(p => p.ClientProfileId == client.Id)
            .Should().HaveCount(2, "history is preserved");
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

        var planA = WeeklyTrainingPlan.Create(clientA.Id, Guid.NewGuid(), gymId, "A");
        var planB = WeeklyTrainingPlan.Create(clientB.Id, Guid.NewGuid(), gymId, "B");
        ctx.WeeklyTrainingPlans.AddRange(planA, planB);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new ActivatePlanHandler(ctx, user);
        await handler.Handle(new ActivatePlanCommand(planA.Id), CancellationToken.None);
        await handler.Handle(new ActivatePlanCommand(planB.Id), CancellationToken.None);

        ctx.WeeklyTrainingPlans.Single(p => p.Id == planA.Id).Status
            .Should().Be(TrainingPlanStatus.Active, "client B's activation must not affect client A");
    }

    [Fact]
    public async Task Draft_And_Archived_Siblings_Are_Left_Alone()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var ctx = TestDbContextFactory.Create(user);

        var client = ClientProfileMother.Create(gymId: gymId, coachId: Guid.NewGuid());
        ctx.ClientProfiles.Add(client);

        var draft = WeeklyTrainingPlan.Create(client.Id, Guid.NewGuid(), gymId, "Draft");
        var archived = WeeklyTrainingPlan.Create(client.Id, Guid.NewGuid(), gymId, "Archived");
        archived.Archive();
        var toActivate = WeeklyTrainingPlan.Create(client.Id, Guid.NewGuid(), gymId, "New");
        ctx.WeeklyTrainingPlans.AddRange(draft, archived, toActivate);
        await ctx.SaveChangesAsync(CancellationToken.None);

        await new ActivatePlanHandler(ctx, user)
            .Handle(new ActivatePlanCommand(toActivate.Id), CancellationToken.None);

        ctx.WeeklyTrainingPlans.Single(p => p.Id == draft.Id).Status
            .Should().Be(TrainingPlanStatus.Draft);
        ctx.WeeklyTrainingPlans.Single(p => p.Id == archived.Id).Status
            .Should().Be(TrainingPlanStatus.Archived);
        ctx.WeeklyTrainingPlans.Single(p => p.Id == toActivate.Id).Status
            .Should().Be(TrainingPlanStatus.Active);
    }
}
