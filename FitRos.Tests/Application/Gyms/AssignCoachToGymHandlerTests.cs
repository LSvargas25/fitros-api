using FitRos.Application.Features.Gyms.AssignCoachToGym;
using FitRos.Application.Features.Gyms.CreateGym;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Gyms;

public class AssignCoachToGymHandlerTests
{
    private static async Task<Guid> CreateGymAsync(
        FitRos.Infrastructure.Persistence.FitRosDbContext context,
        FakeCurrentUser fakeOwner)
    {
        var gymHandler = new CreateGymHandler(context, fakeOwner);
        return await gymHandler.Handle(
            new CreateGymCommand("Test Gym", "Test City", "5551234"),
            CancellationToken.None);
    }

    [Fact]
    public async Task Should_Assign_Coach_To_Gym()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var gymId = await CreateGymAsync(context, fakeOwner);

        var coach = User.Create("coach@test.com", "John", "Coach", "hash", UserRole.Coach);
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new AssignCoachToGymHandler(context, fakeOwner);
        await handler.Handle(new AssignCoachToGymCommand(gymId, coach.Id), CancellationToken.None);

        var inDb = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == coach.Id);
        inDb.GymId.Should().Be(gymId);
    }

    [Fact]
    public async Task Should_Throw_NotFoundException_When_Gym_Not_Found()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var coach = User.Create("coach@test.com", "John", "Coach", "hash", UserRole.Coach);
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new AssignCoachToGymHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new AssignCoachToGymCommand(Guid.NewGuid(), coach.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_NotFoundException_When_Coach_Not_Found()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var gymId = await CreateGymAsync(context, fakeOwner);

        var handler = new AssignCoachToGymHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new AssignCoachToGymCommand(gymId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_DomainException_When_Coach_Is_Inactive()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var gymId = await CreateGymAsync(context, fakeOwner);

        var coach = User.Create("coach@test.com", "John", "Coach", "hash", UserRole.Coach);
        coach.Deactivate();
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new AssignCoachToGymHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new AssignCoachToGymCommand(gymId, coach.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Coach must be active to be assigned to a gym.");
    }

    [Fact]
    public async Task Should_Throw_DomainException_When_Coach_Already_In_Different_Gym()
    {
        var otherGymId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var gymId = await CreateGymAsync(context, fakeOwner);

        var coach = User.CreateForGym(otherGymId, "coach@test.com", "John", "Coach", "hash", UserRole.Coach);
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new AssignCoachToGymHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new AssignCoachToGymCommand(gymId, coach.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Coach is already assigned to a different gym.");
    }

    [Fact]
    public async Task Should_Succeed_When_Coach_Already_In_Same_Gym()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var gymId = await CreateGymAsync(context, fakeOwner);

        var coach = User.CreateForGym(gymId, "coach@test.com", "John", "Coach", "hash", UserRole.Coach);
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new AssignCoachToGymHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new AssignCoachToGymCommand(gymId, coach.Id), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
