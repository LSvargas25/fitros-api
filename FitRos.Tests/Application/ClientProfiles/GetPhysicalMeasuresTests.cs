using System;
using System.Threading;
using System.Threading.Tasks;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.GetPhysicalMeasures;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles;

public class GetPhysicalMeasuresTests
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

        var handler = new GetPhysicalMeasuresQueryHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(
                new GetPhysicalMeasuresQuery(Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Profile_Not_Found()
    {
        var coachId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(true, UserRole.Coach, coachId);

        using var context = CreateContext(currentUser.Object);

        var handler = new GetPhysicalMeasuresQueryHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new GetPhysicalMeasuresQuery(Guid.NewGuid()),
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

        var handler = new GetPhysicalMeasuresQueryHandler(context, mockUser.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new GetPhysicalMeasuresQuery(profile.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Client_Requests_Other_Clients_Measures()
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

        var handler = new GetPhysicalMeasuresQueryHandler(context, mockUser.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new GetPhysicalMeasuresQuery(profile.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Return_Measures_Most_Recent_First()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var mockUser = CreateCurrentUser(true, UserRole.Client, clientUserId, gymId);

        using var context = CreateContext(mockUser.Object);

        var profile = ClientProfile.Create(gymId, clientUserId, coachId);
        profile.AddMeasure(80, 20, 30, 90, 100, 40);

        // Ensure a distinct RecordedAt tick so ordering is deterministic.
        Thread.Sleep(20);

        profile.AddMeasure(78, 19, 31, 88, 100, 40);

        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new GetPhysicalMeasuresQueryHandler(context, mockUser.Object);

        var result = await handler.Handle(
            new GetPhysicalMeasuresQuery(profile.Id),
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(78, result[0].Weight);
        Assert.Equal(80, result[1].Weight);
    }
}
