using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.Gyms.SoftDeleteGym;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Gyms;

public class SoftDeleteApiTest
{
    [Fact]
    public async Task Should_SoftDelete_Gym_And_Create_Audit_And_Outbox()
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

        context.Gyms.Add(gym);

        await context.SaveChangesAsync();

        var handler = new SoftDeleteGymHandler(
            context,
            fakeUser);

        var command = new SoftDeleteGymCommand(gym.Id);

        await handler.Handle(command, CancellationToken.None);

        var deletedGym = context.Gyms
            .IgnoreQueryFilters()
            .First(x => x.Id == gym.Id);

        Assert.True(deletedGym.IsDeleted);
        Assert.False(deletedGym.IsActive);

        var audit = context.AuditLogEntries
            .IgnoreQueryFilters()
            .First();

        Assert.Equal("GymDeleted", audit.EventType);

        var outbox = context.OutboxMessages
            .IgnoreQueryFilters()
            .First();

        Assert.Equal("GymDeleted", outbox.Type);
    }

    [Fact]
    public async Task Should_Throw_Forbidden_When_User_Is_Not_Authorized()
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
            "2222");

        context.Gyms.Add(gym);

        await context.SaveChangesAsync();

        var handler = new SoftDeleteGymHandler(
            context,
            fakeUser);

        var command = new SoftDeleteGymCommand(gym.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Gym_Does_Not_Exist()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var handler = new SoftDeleteGymHandler(
            context,
            fakeUser);

        var command = new SoftDeleteGymCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}