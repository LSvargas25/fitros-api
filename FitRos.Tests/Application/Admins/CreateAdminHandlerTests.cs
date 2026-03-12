using FitRos.Application.Features.Users.Admin.CreateAdmin;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Admins;

public class CreateAdminHandlerTests
{
    [Fact]
    public async Task OwnerApp_Creates_Admin_Without_Gym()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var hasher = new FakePasswordHasher();
        var handler = new CreateAdminHandler(context, hasher);

        var command = new CreateAdminCommand("admin@test.com", "Alice", "Admin", "Password123");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Email.Should().Be("admin@test.com");
        result.Role.Should().Be((int)UserRole.Admin);

        var userInDb = await context.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.Id == result.Id);
        userInDb.Role.Should().Be(UserRole.Admin);
        userInDb.GymId.Should().BeNull();
    }

    [Fact]
    public async Task Throws_On_Duplicate_Email()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var hasher = new FakePasswordHasher();

        var existing = User.Create("dup@test.com", "Existing", "User", "hash", UserRole.Coach);
        context.Users.Add(existing);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateAdminHandler(context, hasher);
        var command = new CreateAdminCommand("dup@test.com", "New", "Admin", "Password123");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Email already exists.");
    }

    [Fact]
    public async Task Notifies_OwnerApp_After_Admin_Created()
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
        await context.SaveChangesAsync(CancellationToken.None);

        var hasher = new FakePasswordHasher();
        var handler = new CreateAdminHandler(context, hasher);
        var command = new CreateAdminCommand("newadmin@test.com", "New", "Admin", "Password123");

        await handler.Handle(command, CancellationToken.None);

        var notifications = context.Notifications.IgnoreQueryFilters().ToList();
        notifications.Should().Contain(n => n.UserId == ownerAppId);
    }
}
