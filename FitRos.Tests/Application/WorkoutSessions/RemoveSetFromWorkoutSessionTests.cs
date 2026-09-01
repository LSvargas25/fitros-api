using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.WorkoutSessions;
using FitRos.Application.Features.WorkoutSessions.AddSetToWorkoutSession;
using FitRos.Application.Features.WorkoutSessions.RemoveSetFromWorkoutSession;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.WorkoutSessions;

public class RemoveSetFromWorkoutSessionTests
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
    public async Task Should_Remove_Set()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(true, UserRole.Client, userId, gymId);

        using var context = CreateContext(currentUser.Object);

        var session = WorkoutSession.Create(
            gymId, userId, Guid.NewGuid(), "Piernas", 1, DateTime.UtcNow.Date);
        session.Start();

        context.WorkoutSessions.Add(session);
        await context.SaveChangesAsync();

        var addHandler = new AddSetToWorkoutSessionHandler(context, currentUser.Object);
        await addHandler.Handle(
            new AddSetToWorkoutSessionCommand(session.Id, exerciseId, 1, 10, 40),
            CancellationToken.None);

        var setId = (await context.WorkoutSessions.IncludeSets().FirstAsync(s => s.Id == session.Id))
            .Sets.First().Id;

        var handler = new RemoveSetFromWorkoutSessionHandler(context, currentUser.Object);
        await handler.Handle(
            new RemoveSetFromWorkoutSessionCommand(session.Id, setId),
            CancellationToken.None);

        var reloaded = await context.WorkoutSessions
            .IncludeSets()
            .FirstAsync(s => s.Id == session.Id);

        Assert.Empty(reloaded.Sets);
    }

    [Fact]
    public async Task Should_Throw_When_Set_Not_Found()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(true, UserRole.Client, userId, gymId);

        using var context = CreateContext(currentUser.Object);

        var session = WorkoutSession.Create(
            gymId, userId, Guid.NewGuid(), "Piernas", 1, DateTime.UtcNow.Date);
        session.Start();
        context.WorkoutSessions.Add(session);
        await context.SaveChangesAsync();

        var handler = new RemoveSetFromWorkoutSessionHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new RemoveSetFromWorkoutSessionCommand(session.Id, Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Not_Owner()
    {
        var gymId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();

        var owningUser = CreateCurrentUser(true, UserRole.Client, ownerUserId, gymId);
        using var context = CreateContext(owningUser.Object);

        var session = WorkoutSession.Create(
            gymId, ownerUserId, Guid.NewGuid(), "Piernas", 1, DateTime.UtcNow.Date);
        session.Start();
        context.WorkoutSessions.Add(session);
        await context.SaveChangesAsync();

        var addHandler = new AddSetToWorkoutSessionHandler(context, owningUser.Object);
        await addHandler.Handle(
            new AddSetToWorkoutSessionCommand(session.Id, exerciseId, 1, 10, 40),
            CancellationToken.None);

        var setId = (await context.WorkoutSessions.IncludeSets().FirstAsync(s => s.Id == session.Id))
            .Sets.First().Id;

        var otherUser = CreateCurrentUser(true, UserRole.Client, otherUserId, gymId);
        var handler = new RemoveSetFromWorkoutSessionHandler(context, otherUser.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new RemoveSetFromWorkoutSessionCommand(session.Id, setId),
                CancellationToken.None));
    }
}
