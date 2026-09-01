using FitRos.Application.Features.ClientProfiles.ProgressReport.GenerateProgressReport;
using FitRos.Application.Features.ClientProfiles.ProgressReport.GetProgressReport;
using FitRos.Application.Features.ClientProfiles.ProgressReport.GetProgressReportHistory;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles;

public class ProgressReportTests
{
    private static FakeCurrentUser Admin(Guid gymId) => new(gymId)
    {
        UserId = Guid.NewGuid(),
        Role = UserRole.Admin,
        IsAuthenticated = true
    };

    // PhysicalMeasure.Create(clientProfileId, weight, bodyFat, muscleMass, waist, chest, arms)
    // RecordedAt is set to UtcNow internally, so back-date it via its backing field for tests.
    private static PhysicalMeasure MeasureAt(Guid clientProfileId, DateTime recordedAtUtc,
        decimal weight, decimal bodyFat, decimal waist)
    {
        var m = PhysicalMeasure.Create(clientProfileId, weight, bodyFat, 30m, waist, 100m, 35m);
        typeof(PhysicalMeasure)
            .GetField("<RecordedAt>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(m, DateTime.SpecifyKind(recordedAtUtc, DateTimeKind.Utc));
        return m;
    }

    private static async Task<ClientProfile> SeedClientWithMeasures(
        FitRosDbContext context, Guid gymId, Guid userId, DateTime thisMonth, DateTime lastMonth)
    {
        var client = ClientProfile.Create(gymId, userId, Guid.NewGuid());
        context.ClientProfiles.Add(client);
        await context.SaveChangesAsync(CancellationToken.None);

        context.PhysicalMeasures.Add(MeasureAt(client.Id, lastMonth, 82m, 22m, 88m));
        context.PhysicalMeasures.Add(MeasureAt(client.Id, thisMonth, 80m, 20m, 85m));
        await context.SaveChangesAsync(CancellationToken.None);

        return client;
    }

    private static async Task AddCompletedSessionWithSets(
        FitRosDbContext context, Guid userId, Guid gymId, DateTime scheduledUtc, int setCount)
    {
        var session = WorkoutSession.Create(gymId, userId, Guid.NewGuid(), "Routine", 1,
            DateTime.SpecifyKind(scheduledUtc, DateTimeKind.Utc));
        session.Start();
        for (var i = 1; i <= setCount; i++)
            session.AddSet(Guid.NewGuid(), i, 10, 50m);
        session.Complete();

        context.WorkoutSessions.Add(session);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Get_Should_Throw_When_Not_Authenticated()
    {
        var currentUser = new Mock<FitRos.Application.Abstractions.Security.ICurrentUser>();
        currentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var context = TestDbContextFactory.Create(currentUser.Object);
        var client = ClientProfile.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        context.ClientProfiles.Add(client);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProgressReportHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GetProgressReportQuery(client.Id, 2026, 8), CancellationToken.None));
    }

    [Fact]
    public async Task Get_Should_Throw_When_Client_Not_Found()
    {
        var user = Admin(Guid.NewGuid());
        var context = TestDbContextFactory.Create(user);
        var handler = new GetProgressReportHandler(context, user);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetProgressReportQuery(Guid.NewGuid(), 2026, 8), CancellationToken.None));
    }

    [Fact]
    public async Task Get_Computes_Month_Over_Month_Deltas_For_Measures_And_Sets()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = Admin(gymId);
        user.UserId = Guid.NewGuid();
        var context = TestDbContextFactory.Create(user);

        // Report month = August 2026, previous = July 2026
        var thisMonth = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
        var lastMonth = new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc);

        var client = await SeedClientWithMeasures(context, gymId, userId, thisMonth, lastMonth);

        await AddCompletedSessionWithSets(context, userId, gymId, thisMonth, 12);
        await AddCompletedSessionWithSets(context, userId, gymId, lastMonth, 8);
        // a skipped/other-month session that must NOT count
        await AddCompletedSessionWithSets(context, userId, gymId, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), 99);

        var handler = new GetProgressReportHandler(context, user);
        var report = await handler.Handle(
            new GetProgressReportQuery(client.Id, 2026, 8), CancellationToken.None);

        report.Year.Should().Be(2026);
        report.Month.Should().Be(8);

        report.Weight.Current.Should().Be(80m);
        report.Weight.Previous.Should().Be(82m);
        report.Weight.Delta.Should().Be(-2m);

        report.BodyFatPercentage.Delta.Should().Be(-2m);
        report.Waist.Delta.Should().Be(-3m);

        report.CompletedSets.Current.Should().Be(12m);
        report.CompletedSets.Previous.Should().Be(8m);
        report.CompletedSets.Delta.Should().Be(4m);
    }

    [Fact]
    public async Task Get_Nulls_Deltas_When_A_Month_Has_No_Measure()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);

        var client = ClientProfile.Create(gymId, userId, Guid.NewGuid());
        context.ClientProfiles.Add(client);
        await context.SaveChangesAsync(CancellationToken.None);

        // only a current-month measure, none for the previous month
        context.PhysicalMeasures.Add(MeasureAt(client.Id,
            new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc), 80m, 20m, 85m));
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProgressReportHandler(context, user);
        var report = await handler.Handle(
            new GetProgressReportQuery(client.Id, 2026, 8), CancellationToken.None);

        report.Weight.Current.Should().Be(80m);
        report.Weight.Previous.Should().BeNull();
        report.Weight.Delta.Should().BeNull();
        report.CompletedSets.Current.Should().Be(0m);
    }

    [Fact]
    public async Task Generate_Persists_A_Snapshot_And_History_Returns_It()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);

        var thisMonth = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
        var lastMonth = new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc);
        var client = await SeedClientWithMeasures(context, gymId, userId, thisMonth, lastMonth);

        var generate = new GenerateProgressReportHandler(context, user);
        var response = await generate.Handle(
            new GenerateProgressReportCommand(client.Id, 2026, 8), CancellationToken.None);

        response.SnapshotId.Should().NotBeEmpty();
        response.PhysicalMeasureId.Should().NotBeEmpty();
        response.Report.Weight.Delta.Should().Be(-2m);

        context.ClientProgressReportSnapshots.Should().ContainSingle(s => s.Id == response.SnapshotId);

        var history = await new GetProgressReportHistoryHandler(context, user)
            .Handle(new GetProgressReportHistoryQuery(client.Id), CancellationToken.None);

        history.Should().ContainSingle();
        history[0].ReportJson.Should().Contain("\"Delta\":-2");
    }

    [Fact]
    public async Task Generate_Throws_When_Client_Has_No_Measures()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);

        var client = ClientProfile.Create(gymId, Guid.NewGuid(), Guid.NewGuid());
        context.ClientProfiles.Add(client);
        await context.SaveChangesAsync(CancellationToken.None);

        var generate = new GenerateProgressReportHandler(context, user);

        await Assert.ThrowsAsync<DomainException>(() =>
            generate.Handle(new GenerateProgressReportCommand(client.Id, 2026, 8), CancellationToken.None));
    }

    [Fact]
    public async Task Coach_Cannot_View_A_Client_They_Do_Not_Own()
    {
        var gymId = Guid.NewGuid();
        var coach = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Coach,
            IsAuthenticated = true
        };
        var context = TestDbContextFactory.Create(coach);

        var client = ClientProfile.Create(gymId, Guid.NewGuid(), Guid.NewGuid()); // different coach
        context.ClientProfiles.Add(client);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProgressReportHandler(context, coach);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetProgressReportQuery(client.Id, 2026, 8), CancellationToken.None));
    }
}
