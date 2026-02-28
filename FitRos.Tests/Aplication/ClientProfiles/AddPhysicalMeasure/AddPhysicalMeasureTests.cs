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

namespace FitRos.Tests.Application.ClientProfiles.AddPhysicalMeasure;

public class AddPhysicalMeasureTests
{
    private static FitRosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FitRosDbContext(options);
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
        using var context = CreateContext();

        var currentUser = CreateCurrentUser(false, UserRole.Coach, Guid.NewGuid());

        var handler = new AddPhysicalMeasureCommandHandler(context, currentUser.Object);

        var command = new AddPhysicalMeasureCommand(
            Guid.NewGuid(), 80, 20, 30, 80, 100, 40);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_User_Is_Not_Coach()
    {
        using var context = CreateContext();

        var currentUser = CreateCurrentUser(true, UserRole.Client, Guid.NewGuid());

        var handler = new AddPhysicalMeasureCommandHandler(context, currentUser.Object);

        var command = new AddPhysicalMeasureCommand(
            Guid.NewGuid(), 80, 20, 30, 80, 100, 40);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Profile_Not_Found()
    {
        using var context = CreateContext();

        var coachId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(true, UserRole.Coach, coachId);

        var handler = new AddPhysicalMeasureCommandHandler(context, currentUser.Object);

        var command = new AddPhysicalMeasureCommand(
            Guid.NewGuid(), 80, 20, 30, 80, 100, 40);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Coach_Not_Owner()
    {
        using var context = CreateContext();

        var realCoachId = Guid.NewGuid();
        var otherCoachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var profile = ClientProfile.Create(clientUserId, realCoachId);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var currentUser = CreateCurrentUser(true, UserRole.Coach, otherCoachId);

        var handler = new AddPhysicalMeasureCommandHandler(context, currentUser.Object);

        var command = new AddPhysicalMeasureCommand(
            profile.Id, 80, 20, 30, 80, 100, 40);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Add_PhysicalMeasure_When_Valid()
    {
        using var context = CreateContext();

        var coachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var profile = ClientProfile.Create(clientUserId, coachId);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var currentUser = CreateCurrentUser(true, UserRole.Coach, coachId);

        var handler = new AddPhysicalMeasureCommandHandler(context, currentUser.Object);

        var command = new AddPhysicalMeasureCommand(
            profile.Id, 80, 20, 30, 80, 100, 40);

        await handler.Handle(command, CancellationToken.None);

        var updated = await context.ClientProfiles
            .Include(x => x.Measures)
            .FirstAsync();

        Assert.Single(updated.Measures);
    }
}