using FitRos.Application.Features.Users.Admin.ActivateAdmin;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Admins;

public class ActivateAdminHandlerTests
{
    [Fact]
    public async Task Should_Activate_Inactive_Admin()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);

        var admin = User.Create("admin@test.com", "Alice", "Admin", "hash", UserRole.Admin);
        admin.Deactivate();
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var handler = new ActivateAdminHandler(context, fakeOwner);

        await handler.Handle(new ActivateAdminCommand(admin.Id), CancellationToken.None);

        var adminInDb = await context.Users
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == admin.Id);

        adminInDb.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task Should_Not_Throw_When_Admin_Already_Active()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);

        var admin = User.Create("admin@active.com", "Bob", "Admin", "hash", UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var handler = new ActivateAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new ActivateAdminCommand(admin.Id), CancellationToken.None);

        await act.Should().NotThrowAsync();

        var adminInDb = await context.Users
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == admin.Id);

        adminInDb.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task Should_Throw_NotFoundException_When_Admin_Not_Found()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var handler = new ActivateAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new ActivateAdminCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_NotFoundException_When_Id_Belongs_To_Non_Admin_Role()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);

        var coach = User.Create("coach@test.com", "Carlos", "Coach", "hash", UserRole.Coach);
        coach.Deactivate();
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new ActivateAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new ActivateAdminCommand(coach.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_UnauthorizedException_When_Not_Authenticated()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = false
        };

        var context = TestDbContextFactory.Create(fakeOwner);

        var admin = User.Create("admin@unauth.com", "Dan", "Admin", "hash", UserRole.Admin);
        admin.Deactivate();
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var handler = new ActivateAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new ActivateAdminCommand(admin.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
