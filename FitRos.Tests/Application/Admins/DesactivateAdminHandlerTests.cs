using FitRos.Application.Features.Users.Admin.DesactivateAdmin;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Admins;

public class DesactivateAdminHandlerTests
{
    [Fact]
    public async Task Should_Deactivate_Active_Admin()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);

        var admin = User.Create("admin@test.com", "Alice", "Admin", "hash", UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var handler = new DesactivateAdminHandler(context, fakeOwner);

        await handler.Handle(new DesactivateAdminCommand(admin.Id), CancellationToken.None);

        var adminInDb = await context.Users
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == admin.Id);

        adminInDb.Status.Should().Be(UserStatus.Inactive);
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
        var handler = new DesactivateAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new DesactivateAdminCommand(Guid.NewGuid()), CancellationToken.None);

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
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new DesactivateAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new DesactivateAdminCommand(coach.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_DomainException_When_Deactivating_Self()
    {
        var adminId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = adminId,
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);

        var admin = User.Create("self@test.com", "Self", "Admin", "hash", UserRole.Admin);

        // Force the admin entity Id to match the current user
        typeof(User)
            .GetProperty(nameof(User.Id))!
            .SetValue(admin, adminId);

        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var handler = new DesactivateAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new DesactivateAdminCommand(adminId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("You cannot deactivate yourself.");
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
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var handler = new DesactivateAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new DesactivateAdminCommand(admin.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
