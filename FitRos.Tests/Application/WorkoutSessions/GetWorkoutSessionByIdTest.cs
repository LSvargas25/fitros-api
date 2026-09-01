using System;
using System.Threading;
using System.Threading.Tasks;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.WorkoutSessions.GetWorkoutSessionById;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.WorkoutSessions;

public class GetWorkoutSessionByIdTest
{
    private static FitRosDbContext CreateContext(ICurrentUser currentUser)
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FitRosDbContext(options, currentUser);
    }

    private static Mock<ICurrentUser> CreateCurrentUser(UserRole role, Guid? userId, Guid? gymId)
    {
        var mock = new Mock<ICurrentUser>();

        mock.Setup(x => x.IsAuthenticated).Returns(true);
        mock.Setup(x => x.Role).Returns(role);
        mock.Setup(x => x.UserId).Returns(userId);
        mock.Setup(x => x.GymId).Returns(gymId);

        return mock;
    }

    private static WorkoutSession CreateSession(Guid gymId, Guid clientUserId)
    {
        var session = WorkoutSession.Create(
            gymId, clientUserId, Guid.NewGuid(), "Piernas", 1, DateTime.UtcNow.Date);
        session.Start();
        return session;
    }

    [Fact]
    public async Task Client_can_access_own_session()
    {
        var gymId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var currentUser = CreateCurrentUser(UserRole.Client, clientUserId, gymId);
        using var context = CreateContext(currentUser.Object);

        var session = CreateSession(gymId, clientUserId);
        context.WorkoutSessions.Add(session);
        await context.SaveChangesAsync();

        var handler = new GetWorkoutSessionByIdHandler(context, currentUser.Object);

        var result = await handler.Handle(
            new GetWorkoutSessionByIdQuery(session.Id), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Unrelated_coach_in_same_gym_cannot_access_session()
    {
        var gymId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var assignedCoachId = Guid.NewGuid();
        var otherCoachId = Guid.NewGuid();

        var otherCoach = CreateCurrentUser(UserRole.Coach, otherCoachId, gymId);
        using var context = CreateContext(otherCoach.Object);

        var session = CreateSession(gymId, clientUserId);
        context.WorkoutSessions.Add(session);

        var profile = ClientProfile.Create(gymId, clientUserId, assignedCoachId);
        context.ClientProfiles.Add(profile);

        await context.SaveChangesAsync();

        var handler = new GetWorkoutSessionByIdHandler(context, otherCoach.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetWorkoutSessionByIdQuery(session.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Assigned_coach_can_access_session()
    {
        var gymId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var coach = CreateCurrentUser(UserRole.Coach, coachId, gymId);
        using var context = CreateContext(coach.Object);

        var session = CreateSession(gymId, clientUserId);
        context.WorkoutSessions.Add(session);

        var profile = ClientProfile.Create(gymId, clientUserId, coachId);
        context.ClientProfiles.Add(profile);

        await context.SaveChangesAsync();

        var handler = new GetWorkoutSessionByIdHandler(context, coach.Object);

        var result = await handler.Handle(
            new GetWorkoutSessionByIdQuery(session.Id), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Admin_from_other_gym_cannot_access_session()
    {
        var gymId = Guid.NewGuid();
        var otherGymId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var admin = CreateCurrentUser(UserRole.Admin, Guid.NewGuid(), otherGymId);
        using var context = CreateContext(admin.Object);

        var session = CreateSession(gymId, clientUserId);
        context.WorkoutSessions.Add(session);
        await context.SaveChangesAsync();

        var handler = new GetWorkoutSessionByIdHandler(context, admin.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetWorkoutSessionByIdQuery(session.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Owner_can_access_session_in_any_gym()
    {
        var gymId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var owner = CreateCurrentUser(UserRole.OwnerApp, Guid.NewGuid(), null);
        using var context = CreateContext(owner.Object);

        var session = CreateSession(gymId, clientUserId);
        context.WorkoutSessions.Add(session);
        await context.SaveChangesAsync();

        var handler = new GetWorkoutSessionByIdHandler(context, owner.Object);

        var result = await handler.Handle(
            new GetWorkoutSessionByIdQuery(session.Id), CancellationToken.None);

        Assert.NotNull(result);
    }
}
