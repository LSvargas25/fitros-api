using System;
using System.Threading;
using System.Threading.Tasks;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.GenerateKpiSnapshot;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles;

public class GenerateClientKpiSnapshotTests
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

        var handler = new GenerateClientKpiSnapshotCommandHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(
                new GenerateClientKpiSnapshotCommand(Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Profile_Not_Found()
    {
        var coachId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(true, UserRole.Coach, coachId);

        using var context = CreateContext(currentUser.Object);

        var handler = new GenerateClientKpiSnapshotCommandHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new GenerateClientKpiSnapshotCommand(Guid.NewGuid()),
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
        profile.AddMeasure(80, 20, 30, 80, 100, 40);

        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new GenerateClientKpiSnapshotCommandHandler(context, mockUser.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new GenerateClientKpiSnapshotCommand(profile.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Client_Generates_Snapshot_For_Other_Client()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var otherClientUserId = Guid.NewGuid();

        var mockUser = CreateCurrentUser(true, UserRole.Client, otherClientUserId, gymId);

        using var context = CreateContext(mockUser.Object);

        var profile = ClientProfile.Create(gymId, clientUserId, coachId);
        profile.AddMeasure(80, 20, 30, 80, 100, 40);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new GenerateClientKpiSnapshotCommandHandler(context, mockUser.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new GenerateClientKpiSnapshotCommand(profile.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Generate_Snapshot_When_Client_Generates_For_Own_Profile()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var mockUser = CreateCurrentUser(true, UserRole.Client, clientUserId, gymId);

        using var context = CreateContext(mockUser.Object);

        var profile = ClientProfile.Create(gymId, clientUserId, coachId);
        profile.AddMeasure(80, 20, 30, 80, 100, 40);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new GenerateClientKpiSnapshotCommandHandler(context, mockUser.Object);

        var result = await handler.Handle(
            new GenerateClientKpiSnapshotCommand(profile.Id),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task Should_Throw_When_No_Measures_Exist()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var mockUser = CreateCurrentUser(true, UserRole.Coach, coachId, gymId);

        using var context = CreateContext(mockUser.Object);

        var profile = ClientProfile.Create(gymId, clientUserId, coachId);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new GenerateClientKpiSnapshotCommandHandler(context, mockUser.Object);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                new GenerateClientKpiSnapshotCommand(profile.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Generate_Snapshot_With_Null_Deltas_When_Only_One_Measure()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var mockUser = CreateCurrentUser(true, UserRole.Coach, coachId, gymId);

        using var context = CreateContext(mockUser.Object);

        var profile = ClientProfile.Create(gymId, clientUserId, coachId);
        profile.AddMeasure(80, 20, 30, 80, 100, 40);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new GenerateClientKpiSnapshotCommandHandler(context, mockUser.Object);

        var result = await handler.Handle(
            new GenerateClientKpiSnapshotCommand(profile.Id),
            CancellationToken.None);

        Assert.Null(result.WeightDelta);
        Assert.Null(result.BodyFatDelta);
        Assert.Null(result.WaistDelta);

        Assert.Single(context.ClientKpiSnapshots);
    }

    [Fact]
    public async Task Should_Generate_Snapshot_With_Deltas_When_Two_Measures()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var mockUser = CreateCurrentUser(true, UserRole.Coach, coachId, gymId);

        using var context = CreateContext(mockUser.Object);

        var profile = ClientProfile.Create(gymId, clientUserId, coachId);
        profile.AddMeasure(weight: 80, bodyFatPercentage: 20, muscleMass: 30, waist: 90, chest: 100, arms: 40);

        // Ensure a distinct RecordedAt tick so ordering is deterministic.
        Thread.Sleep(20);

        profile.AddMeasure(weight: 78, bodyFatPercentage: 19, muscleMass: 31, waist: 88, chest: 100, arms: 40);

        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new GenerateClientKpiSnapshotCommandHandler(context, mockUser.Object);

        var result = await handler.Handle(
            new GenerateClientKpiSnapshotCommand(profile.Id),
            CancellationToken.None);

        Assert.Equal(-2, result.WeightDelta);
        Assert.Equal(-1, result.BodyFatDelta);
        Assert.Equal(-2, result.WaistDelta);
    }
}
