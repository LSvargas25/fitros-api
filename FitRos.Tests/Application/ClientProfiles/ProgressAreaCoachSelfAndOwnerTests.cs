using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.AddPhysicalMeasure;
using FitRos.Application.Features.ClientProfiles.GenerateKpiSnapshot;
using FitRos.Application.Features.ClientProfiles.GetKpiSnapshots;
using FitRos.Application.Features.ClientProfiles.GetPhysicalMeasures;
using FitRos.Application.Features.ClientProfiles.ProgressReport.GetProgressReport;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles;

// BACKEND_AUDIT §26.2 / §27 — the progress-area handlers now (a) let a Coach act
// on their OWN CoachSelf profile and (b) let OwnerApp through on the two measures
// handlers that previously 403'd them.
public class ProgressAreaCoachSelfAndOwnerTests
{
    private static FitRosDbContext Ctx(ICurrentUser u)
        => new(new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, u);

    private static FakeCurrentUser U(UserRole role, Guid? gymId, Guid userId)
        => new(gymId) { UserId = userId, Role = role, IsAuthenticated = true };

    // ---- Coach on their own CoachSelf profile ----

    [Fact]
    public async Task Coach_can_add_and_read_measures_on_their_own_CoachSelf_profile()
    {
        var gymId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var coach = U(UserRole.Coach, gymId, coachUserId);
        using var ctx = Ctx(coach);

        var self = ClientProfile.CreateForCoachSelf(gymId, coachUserId); // CoachId == null
        ctx.ClientProfiles.Add(self);
        await ctx.SaveChangesAsync();

        await new AddPhysicalMeasureCommandHandler(ctx, coach).Handle(
            new AddPhysicalMeasureCommand(self.Id, 82, 18, 34, 82, 102, 40), CancellationToken.None);

        var history = await new GetPhysicalMeasuresQueryHandler(ctx, coach).Handle(
            new GetPhysicalMeasuresQuery(self.Id), CancellationToken.None);

        history.Should().ContainSingle().Which.Weight.Should().Be(82);
    }

    [Fact]
    public async Task Coach_can_generate_and_read_kpi_and_progress_on_their_own_profile()
    {
        var gymId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var coach = U(UserRole.Coach, gymId, coachUserId);
        using var ctx = Ctx(coach);

        var self = ClientProfile.CreateForCoachSelf(gymId, coachUserId);
        ctx.ClientProfiles.Add(self);
        await ctx.SaveChangesAsync();

        await new AddPhysicalMeasureCommandHandler(ctx, coach).Handle(
            new AddPhysicalMeasureCommand(self.Id, 82, 18, 34, 82, 102, 40), CancellationToken.None);

        await new GenerateClientKpiSnapshotCommandHandler(ctx, coach)
            .Invoking(h => h.Handle(new GenerateClientKpiSnapshotCommand(self.Id), CancellationToken.None))
            .Should().NotThrowAsync();

        var kpis = await new GetClientKpiSnapshotsHandler(ctx, coach).Handle(
            new GetClientKpiSnapshotsQuery(self.Id), CancellationToken.None);
        kpis.Should().ContainSingle();

        await new GetProgressReportHandler(ctx, coach)
            .Invoking(h => h.Handle(new GetProgressReportQuery(self.Id, null, null), CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task Coach_still_403s_on_another_coachs_self_profile()
    {
        var gymId = Guid.NewGuid();
        var coach = U(UserRole.Coach, gymId, Guid.NewGuid());
        using var ctx = Ctx(coach);

        var otherSelf = ClientProfile.CreateForCoachSelf(gymId, Guid.NewGuid());
        ctx.ClientProfiles.Add(otherSelf);
        await ctx.SaveChangesAsync();

        await new GetPhysicalMeasuresQueryHandler(ctx, coach)
            .Invoking(h => h.Handle(new GetPhysicalMeasuresQuery(otherSelf.Id), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
    }

    // ---- OwnerApp on the two measures handlers ----

    [Fact]
    public async Task OwnerApp_can_add_and_read_measures_for_any_client()
    {
        var gymId = Guid.NewGuid();
        var owner = U(UserRole.OwnerApp, null, Guid.NewGuid()); // OwnerApp has no gym
        using var ctx = Ctx(owner);

        var client = ClientProfile.Create(gymId, Guid.NewGuid(), Guid.NewGuid());
        ctx.ClientProfiles.Add(client);
        await ctx.SaveChangesAsync();

        await new AddPhysicalMeasureCommandHandler(ctx, owner)
            .Invoking(h => h.Handle(
                new AddPhysicalMeasureCommand(client.Id, 80, 20, 30, 80, 100, 40), CancellationToken.None))
            .Should().NotThrowAsync();

        var history = await new GetPhysicalMeasuresQueryHandler(ctx, owner).Handle(
            new GetPhysicalMeasuresQuery(client.Id), CancellationToken.None);
        history.Should().ContainSingle();
    }
}
