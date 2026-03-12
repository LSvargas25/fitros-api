using FitRos.Application.Features.Gyms.AssignAdminToGym;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Gyms;

public class AssignAdminToGymHandlerTests
{
    [Fact]
    public async Task OwnerApp_Can_Assign_Unassigned_Admin_To_Gym()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var gym = Gym.Create("Test Gym", "San Jose", "8888-0001");
        context.Gyms.Add(gym);

        var admin = User.Create("admin@test.com", "John", "Doe", "hash", UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AssignAdminToGymHandler(context);

        await handler.Handle(new AssignAdminToGymCommand(gym.Id, admin.Id), CancellationToken.None);

        var updatedAdmin = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == admin.Id);
        updatedAdmin.GymId.Should().Be(gym.Id);
    }

    [Fact]
    public async Task Throws_When_Admin_Already_Has_Gym()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var gym = Gym.Create("Test Gym", "San Jose", "8888-0001");
        context.Gyms.Add(gym);

        var existingGymId = Guid.NewGuid();
        var admin = User.CreateForGym(existingGymId, "admin@test.com", "Jane", "Smith", "hash", UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AssignAdminToGymHandler(context);

        var act = async () => await handler.Handle(
            new AssignAdminToGymCommand(gym.Id, admin.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Admin is already assigned to a gym.");
    }

    [Fact]
    public async Task Throws_When_User_Is_Not_Admin_Role()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var gym = Gym.Create("Test Gym", "San Jose", "8888-0001");
        context.Gyms.Add(gym);

        var coach = User.Create("coach@test.com", "Tom", "Jones", "hash", UserRole.Coach);
        context.Users.Add(coach);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AssignAdminToGymHandler(context);

        var act = async () => await handler.Handle(
            new AssignAdminToGymCommand(gym.Id, coach.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("User is not an Admin.");
    }

    [Fact]
    public async Task Notifies_OwnerApp_After_Assignment()
    {
        var ownerAppId = Guid.NewGuid();
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = ownerAppId,
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var ownerApp = User.CreateOwnerApp(ownerAppId, "owner@fitros.com", "Owner", "App", "hash");
        context.Users.Add(ownerApp);

        var gym = Gym.Create("Notify Gym", "San Jose", "9999-0001");
        context.Gyms.Add(gym);

        var admin = User.Create("admin@notify.com", "Notify", "Admin", "hash", UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AssignAdminToGymHandler(context);
        await handler.Handle(new AssignAdminToGymCommand(gym.Id, admin.Id), CancellationToken.None);

        var notifications = context.Notifications.IgnoreQueryFilters().ToList();
        notifications.Should().HaveCountGreaterThan(0);
        notifications.Should().Contain(n => n.UserId == ownerAppId);
    }
}
