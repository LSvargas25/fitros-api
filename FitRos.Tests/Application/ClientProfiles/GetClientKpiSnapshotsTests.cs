using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.GetKpiSnapshots;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Analytics;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles;

public class GetClientKpiSnapshotsTests
{
    private static FitRosDbContext CreateContext(ICurrentUser currentUser)
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FitRosDbContext(options, currentUser);
    }

    private static Mock<ICurrentUser> CreateCurrentUser(
        bool isAuthenticated,
        UserRole role,
        Guid? userId,
        Guid? gymId = null)
    {
        var mock = new Mock<ICurrentUser>();

        mock.Setup(x => x.IsAuthenticated).Returns(isAuthenticated);
        mock.Setup(x => x.Role).Returns(role);
        mock.Setup(x => x.UserId).Returns(userId);
        mock.Setup(x => x.GymId).Returns(gymId);

        return mock;
    }

    [Fact]
    public async Task Should_Throw_When_User_Not_Authenticated()
    {
        var currentUser = CreateCurrentUser(false, UserRole.Coach, Guid.NewGuid());

        using var context = CreateContext(currentUser.Object);

        var handler = new GetClientKpiSnapshotsHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(
                new GetClientKpiSnapshotsQuery(Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Profile_Not_Found()
    {
        var coachId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(true, UserRole.Coach, coachId);

        using var context = CreateContext(currentUser.Object);

        var handler = new GetClientKpiSnapshotsHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new GetClientKpiSnapshotsQuery(Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Coach_Not_Owner()
    {
        var gymId = Guid.NewGuid();
        var realCoachId = Guid.NewGuid();
        var otherCoachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var mockUser = CreateCurrentUser(true, UserRole.Coach, otherCoachId, gymId);

        using var context = CreateContext(mockUser.Object);

        var profile = ClientProfile.Create(gymId, clientUserId, realCoachId);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new GetClientKpiSnapshotsHandler(context, mockUser.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new GetClientKpiSnapshotsQuery(profile.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Client_Requests_Other_Clients_Snapshots()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var otherClientUserId = Guid.NewGuid();

        var mockUser = CreateCurrentUser(true, UserRole.Client, otherClientUserId, gymId);

        using var context = CreateContext(mockUser.Object);

        var profile = ClientProfile.Create(gymId, clientUserId, coachId);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new GetClientKpiSnapshotsHandler(context, mockUser.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new GetClientKpiSnapshotsQuery(profile.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Return_Own_Snapshots_When_Client_Requests_Own_Profile()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var mockUser = CreateCurrentUser(true, UserRole.Client, clientUserId, gymId);

        using var context = CreateContext(mockUser.Object);

        var profile = ClientProfile.Create(gymId, clientUserId, coachId);
        profile.AddMeasure(80, 20, 30, 90, 100, 40);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var measureId = profile.Measures.First().Id;
        var snapshot = ClientKpiSnapshot.Create(profile.Id, measureId, 1, 1, 1);
        context.ClientKpiSnapshots.Add(snapshot);
        await context.SaveChangesAsync();

        var handler = new GetClientKpiSnapshotsHandler(context, mockUser.Object);

        var result = await handler.Handle(
            new GetClientKpiSnapshotsQuery(profile.Id),
            CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Should_Return_Snapshots_Most_Recent_First()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var mockUser = CreateCurrentUser(true, UserRole.Coach, coachId, gymId);

        using var context = CreateContext(mockUser.Object);

        var profile = ClientProfile.Create(gymId, clientUserId, coachId);
        profile.AddMeasure(80, 20, 30, 90, 100, 40);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var measureId = profile.Measures.First().Id;

        var older = ClientKpiSnapshot.Create(profile.Id, measureId, 1, 1, 1);
        var newer = ClientKpiSnapshot.Create(profile.Id, measureId, -2, -1, -2);

        context.ClientKpiSnapshots.Add(older);
        await context.SaveChangesAsync();

        // Ensure a distinct CreatedAtUtc tick so ordering is deterministic.
        Thread.Sleep(20);

        context.ClientKpiSnapshots.Add(newer);
        await context.SaveChangesAsync();

        var handler = new GetClientKpiSnapshotsHandler(context, mockUser.Object);

        var result = await handler.Handle(
            new GetClientKpiSnapshotsQuery(profile.Id),
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }

    [Fact]
    public async Task Owner_Can_Read_Snapshots_Of_A_Client_In_Any_Gym()
    {
        // Snapshots are ITenantEntity, so the global filter is GymId == currentUser.GymId.
        // An Owner has GymId == null; before the fix this handler both rejected the
        // Owner on the gym pre-check and (had it passed) returned an empty list.
        var dbName = Guid.NewGuid().ToString();
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        Guid profileId;
        Guid snapshotId;

        var coach = CreateCurrentUser(true, UserRole.Coach, coachId, gymId);
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        using (var seed = new FitRosDbContext(options, coach.Object))
        {
            var profile = ClientProfile.Create(gymId, clientUserId, coachId);
            profile.AddMeasure(80, 20, 30, 90, 100, 40);
            seed.ClientProfiles.Add(profile);
            await seed.SaveChangesAsync();

            var snap = ClientKpiSnapshot.Create(profile.Id, profile.Measures.First().Id, -2, -1, -2);
            seed.ClientKpiSnapshots.Add(snap);
            await seed.SaveChangesAsync();

            profileId = profile.Id;
            snapshotId = snap.Id;
        }

        var owner = CreateCurrentUser(true, UserRole.OwnerApp, Guid.NewGuid(), gymId: null);
        using var context = new FitRosDbContext(options, owner.Object);
        var handler = new GetClientKpiSnapshotsHandler(context, owner.Object);

        var result = await handler.Handle(
            new GetClientKpiSnapshotsQuery(profileId),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(snapshotId, result[0].Id);
    }
}
