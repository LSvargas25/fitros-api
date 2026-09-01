using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.WorkoutSessions;
using FitRos.Application.Features.WorkoutSessions.AddSetToWorkoutSession;
using FitRos.Application.Features.WorkoutSessions.UpdateSetInWorkoutSession;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.WorkoutSessions;

public class UpdateSetInWorkoutSessionTests
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
    public async Task Should_Update_Set()
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

        var handler = new UpdateSetInWorkoutSessionHandler(context, currentUser.Object);
        await handler.Handle(
            new UpdateSetInWorkoutSessionCommand(session.Id, setId, 1, 12, 45),
            CancellationToken.None);

        var reloaded = await context.WorkoutSessions
            .IncludeSets()
            .FirstAsync(s => s.Id == session.Id);

        Assert.Single(reloaded.Sets);
        Assert.Equal(12, reloaded.Sets.First().RepsAchieved);
        Assert.Equal(45, reloaded.Sets.First().WeightUsed);
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

        var handler = new UpdateSetInWorkoutSessionHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new UpdateSetInWorkoutSessionCommand(session.Id, Guid.NewGuid(), 1, 10, 40),
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
        var handler = new UpdateSetInWorkoutSessionHandler(context, otherUser.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new UpdateSetInWorkoutSessionCommand(session.Id, setId, 1, 12, 45),
                CancellationToken.None));
    }
}
