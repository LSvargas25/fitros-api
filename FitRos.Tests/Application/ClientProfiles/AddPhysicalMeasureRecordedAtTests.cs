using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.AddPhysicalMeasure;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles;

// BACKEND_AUDIT §26.4 / §27 — AddPhysicalMeasure now accepts an optional
// RecordedAt (back-date a forgotten entry): default now, never future, max 60d back.
public class AddPhysicalMeasureRecordedAtTests
{
    private static FitRosDbContext Ctx(ICurrentUser u)
        => new(new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, u);

    private static async Task<(FitRosDbContext ctx, FakeCurrentUser user, Guid profileId)> SeedAsync()
    {
        var gymId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var user = new FakeCurrentUser(gymId)
        { UserId = clientUserId, Role = UserRole.Client, IsAuthenticated = true };
        var ctx = Ctx(user);
        var profile = ClientProfile.Create(gymId, clientUserId, Guid.NewGuid());
        ctx.ClientProfiles.Add(profile);
        await ctx.SaveChangesAsync();
        return (ctx, user, profile.Id);
    }

    [Fact]
    public async Task Omitting_RecordedAt_stamps_now()
    {
        var (ctx, user, profileId) = await SeedAsync();
        var before = DateTime.UtcNow;

        await new AddPhysicalMeasureCommandHandler(ctx, user).Handle(
            new AddPhysicalMeasureCommand(profileId, 80, 20, 30, 80, 100, 40), CancellationToken.None);

        var m = await ctx.PhysicalMeasures.SingleAsync();
        m.RecordedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task A_back_dated_measure_within_the_window_keeps_its_date()
    {
        var (ctx, user, profileId) = await SeedAsync();
        var when = DateTime.UtcNow.AddDays(-10);

        await new AddPhysicalMeasureCommandHandler(ctx, user).Handle(
            new AddPhysicalMeasureCommand(profileId, 80, 20, 30, 80, 100, 40, when), CancellationToken.None);

        (await ctx.PhysicalMeasures.SingleAsync()).RecordedAt.Should().BeCloseTo(when, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void A_future_RecordedAt_fails_validation()
    {
        var result = new AddPhysicalMeasureCommandValidator().Validate(
            new AddPhysicalMeasureCommand(Guid.NewGuid(), 80, 20, 30, 80, 100, 40, DateTime.UtcNow.AddDays(1)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("RecordedAt"));
    }

    [Fact]
    public void A_RecordedAt_older_than_60_days_fails_validation()
    {
        var result = new AddPhysicalMeasureCommandValidator().Validate(
            new AddPhysicalMeasureCommand(Guid.NewGuid(), 80, 20, 30, 80, 100, 40, DateTime.UtcNow.AddDays(-61)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void The_domain_rejects_a_future_measure_regardless_of_the_validator()
    {
        FluentActions.Invoking(() => PhysicalMeasure.Create(
                Guid.NewGuid(), 80, 20, 30, 80, 100, 40, DateTime.UtcNow.AddHours(2)))
            .Should().Throw<DomainException>();
    }
}
