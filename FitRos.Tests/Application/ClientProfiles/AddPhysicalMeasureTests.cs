using System;
using System.Threading;
using System.Threading.Tasks;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.AddPhysicalMeasure;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles;

public class AddPhysicalMeasureTests
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
        Guid? userId)
    {
        var mock = new Mock<ICurrentUser>();
        mock.Setup(x => x.IsAuthenticated).Returns(isAuthenticated);
        mock.Setup(x => x.Role).Returns(role);
        mock.Setup(x => x.UserId).Returns(userId);
        return mock;
    }

    [Fact]
    public async Task Should_Throw_When_User_Not_Authenticated()
    {
        var currentUser = CreateCurrentUser(false, UserRole.Coach, Guid.NewGuid());

        using var context = CreateContext(currentUser.Object);

        var handler = new AddPhysicalMeasureCommandHandler(context, currentUser.Object);

        var command = new AddPhysicalMeasureCommand(
            Guid.NewGuid(), 80, 20, 30, 80, 100, 40);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_User_Is_Not_Coach()
    {
        var currentUser = CreateCurrentUser(true, UserRole.Client, Guid.NewGuid());

        using var context = CreateContext(currentUser.Object);

        var handler = new AddPhysicalMeasureCommandHandler(context, currentUser.Object);

        var command = new AddPhysicalMeasureCommand(
            Guid.NewGuid(), 80, 20, 30, 80, 100, 40);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Profile_Not_Found()
    {
        var coachId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(true, UserRole.Coach, coachId);

        using var context = CreateContext(currentUser.Object);

        var handler = new AddPhysicalMeasureCommandHandler(context, currentUser.Object);

        var command = new AddPhysicalMeasureCommand(
            Guid.NewGuid(), 80, 20, 30, 80, 100, 40);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Coach_Not_Owner()
    {
        var realCoachId = Guid.NewGuid();
        var otherCoachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var mockUser = CreateCurrentUser(true, UserRole.Coach, otherCoachId);

        using var context = CreateContext(mockUser.Object);

        var profile = ClientProfile.Create(clientUserId, realCoachId);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new AddPhysicalMeasureCommandHandler(context, mockUser.Object);

        var command = new AddPhysicalMeasureCommand(
            profile.Id, 80, 20, 30, 80, 100, 40);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Add_PhysicalMeasure_When_Valid()
    {
        var coachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var mockUser = CreateCurrentUser(true, UserRole.Coach, coachId);

        using var context = CreateContext(mockUser.Object);

        var profile = ClientProfile.Create(clientUserId, coachId);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new AddPhysicalMeasureCommandHandler(context, mockUser.Object);

        var command = new AddPhysicalMeasureCommand(
            profile.Id, 80, 20, 30, 80, 100, 40);

        await handler.Handle(command, CancellationToken.None);

        var updated = await context.ClientProfiles
            .Include(x => x.Measures)
            .FirstAsync();

        Assert.Single(updated.Measures);
    }
}