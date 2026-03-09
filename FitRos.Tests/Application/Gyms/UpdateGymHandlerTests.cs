using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.Gyms.UpdateGym;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Gyms;

public class UpdateGymHandlerTests
{
    [Fact]
    public async Task Should_Update_Gym_And_Create_Audit_And_Outbox()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var gym = Gym.Create(
            "Old Gym",
            "Old Address",
            "1111");

        context.Gyms.Add(gym);

        await context.SaveChangesAsync();

        var handler = new UpdateGymHandler(
            context,
            fakeUser);

        var command = new UpdateGymCommand(
            gym.Id,
            "New Gym",
            "New Address",
            "9999");

        await handler.Handle(command, CancellationToken.None);

        var updatedGym = context.Gyms
            .IgnoreQueryFilters()
            .First(x => x.Id == gym.Id);

        Assert.Equal("New Gym", updatedGym.Name);
        Assert.Equal("New Address", updatedGym.Address);
        Assert.Equal("9999", updatedGym.PhoneNumber);

        var audit = context.AuditLogEntries
            .IgnoreQueryFilters()
            .First();

        Assert.Equal("GymUpdated", audit.EventType);

        var outbox = context.OutboxMessages
            .IgnoreQueryFilters()
            .First();

        Assert.Equal("GymUpdated", outbox.Type);
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

        var handler = new UpdateGymHandler(
            context,
            fakeUser);

        var command = new UpdateGymCommand(
            gym.Id,
            "Updated",
            "Updated Address",
            "3333");

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

        var handler = new UpdateGymHandler(
            context,
            fakeUser);

        var command = new UpdateGymCommand(
            Guid.NewGuid(),
            "Gym",
            "Address",
            "9999");

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}