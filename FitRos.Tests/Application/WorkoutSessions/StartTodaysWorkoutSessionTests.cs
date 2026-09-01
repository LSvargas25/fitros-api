using System;
using System.Threading;
using System.Threading.Tasks;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.WorkoutSessions.StartTodaysWorkoutSession;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.WorkoutSessions;

public class StartTodaysWorkoutSessionTests
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
    public async Task Should_Throw_When_Not_Authenticated()
    {
        var currentUser = CreateCurrentUser(false, UserRole.Client, Guid.NewGuid());

        using var context = CreateContext(currentUser.Object);

        var handler = new StartTodaysWorkoutSessionHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new StartTodaysWorkoutSessionCommand(null), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_No_Client_Profile()
    {
        var currentUser = CreateCurrentUser(true, UserRole.Client, Guid.NewGuid(), Guid.NewGuid());

        using var context = CreateContext(currentUser.Object);

        var handler = new StartTodaysWorkoutSessionHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new StartTodaysWorkoutSessionCommand(null), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_No_RoutineId_And_No_Active_Plan()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(true, UserRole.Client, userId, gymId);

        using var context = CreateContext(currentUser.Object);

        var profile = ClientProfile.Create(gymId, userId);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new StartTodaysWorkoutSessionHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new StartTodaysWorkoutSessionCommand(null), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Create_Session_With_Explicit_RoutineId()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(true, UserRole.Client, userId, gymId);

        using var context = CreateContext(currentUser.Object);

        var profile = ClientProfile.Create(gymId, userId);
        context.ClientProfiles.Add(profile);

        var routine = WorkoutRoutine.Create(gymId, "Piernas", "Rutina de piernas");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 60);
        routine.Publish();
        context.WorkoutRoutines.Add(routine);

        await context.SaveChangesAsync();

        var handler = new StartTodaysWorkoutSessionHandler(context, currentUser.Object);

        var result = await handler.Handle(
            new StartTodaysWorkoutSessionCommand(routine.Id), CancellationToken.None);

        Assert.Equal(routine.Id, result.RoutineId);
        Assert.Equal(WorkoutSessionStatus.InProgress, result.Status);
        Assert.Single(result.SuggestedExercises);
    }

    [Fact]
    public async Task Should_Return_Existing_Session_When_Called_Again_Same_Day()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(true, UserRole.Client, userId, gymId);

        using var context = CreateContext(currentUser.Object);

        var profile = ClientProfile.Create(gymId, userId);
        context.ClientProfiles.Add(profile);

        var routine = WorkoutRoutine.Create(gymId, "Piernas", "Rutina de piernas");
        routine.AddExercise(Guid.NewGuid(), 1, 4, 10, 60);
        routine.Publish();
        context.WorkoutRoutines.Add(routine);

        await context.SaveChangesAsync();

        var handler = new StartTodaysWorkoutSessionHandler(context, currentUser.Object);

        var first = await handler.Handle(
            new StartTodaysWorkoutSessionCommand(routine.Id), CancellationToken.None);

        var second = await handler.Handle(
            new StartTodaysWorkoutSessionCommand(null), CancellationToken.None);

        Assert.Equal(first.Id, second.Id);

        var count = await context.WorkoutSessions.CountAsync();
        Assert.Equal(1, count);
    }
}
