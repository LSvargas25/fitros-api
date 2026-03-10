using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.Gyms.ActivateGym;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Gyms;

public class ActivateGymHandlerTests
{
    [Fact]
    public async Task Should_Activate_Gym_And_Create_Audit_And_Outbox()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var gym = Gym.Create(
            "Gym",
            "Address",
            "1111");

        gym.Deactivate();

        context.Gyms.Add(gym);

        await context.SaveChangesAsync();

        var handler = new ActivateGymHandler(
            context,
            fakeUser);

        var command = new ActivateGymCommand(gym.Id);

        await handler.Handle(command, CancellationToken.None);

        var updatedGym = context.Gyms
            .IgnoreQueryFilters()
            .First(x => x.Id == gym.Id);

        Assert.True(updatedGym.IsActive);

        var audit = context.AuditLogEntries
            .IgnoreQueryFilters()
            .First();

        Assert.Equal("GymActivated", audit.EventType);

        var outbox = context.OutboxMessages
            .IgnoreQueryFilters()
            .First();

        Assert.Equal("GymActivated", outbox.Type);
    }

    [Fact]
    public async Task Should_Throw_When_User_Not_Authorized()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Client,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var gym = Gym.Create(
            "Gym",
            "Address",
            "1111");

        context.Gyms.Add(gym);

        await context.SaveChangesAsync();

        var handler = new ActivateGymHandler(
            context,
            fakeUser);

        var command = new ActivateGymCommand(gym.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Gym_Not_Found()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var handler = new ActivateGymHandler(
            context,
            fakeUser);

        var command = new ActivateGymCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}