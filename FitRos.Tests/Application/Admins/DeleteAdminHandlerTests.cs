using FitRos.Application.Features.Users.Admin.DeleteAdmin;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Admins;

public class DeleteAdminHandlerTests
{
    [Fact]
    public async Task Should_Delete_Inactive_Admin_When_Another_Admin_Exists()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);

        var adminToDelete = User.Create("delete@test.com", "Delete", "Me", "hash", UserRole.Admin);
        adminToDelete.Deactivate();

        var remainingAdmin = User.Create("stay@test.com", "Stay", "Admin", "hash", UserRole.Admin);

        context.Users.AddRange(adminToDelete, remainingAdmin);
        await context.SaveChangesAsync();

        var handler = new DeleteAdminHandler(context, fakeOwner);

        await handler.Handle(new DeleteAdminCommand(adminToDelete.Id), CancellationToken.None);

        var exists = await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id == adminToDelete.Id);

        exists.Should().BeFalse();
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
        var handler = new DeleteAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new DeleteAdminCommand(Guid.NewGuid()), CancellationToken.None);

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

        var handler = new DeleteAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new DeleteAdminCommand(coach.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_DomainException_When_Admin_Is_Still_Active()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);

        var admin = User.Create("active@test.com", "Active", "Admin", "hash", UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var handler = new DeleteAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new DeleteAdminCommand(admin.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Admin must be deactivated before permanent deletion.");
    }

    [Fact]
    public async Task Should_Throw_DomainException_When_Deleting_Last_Admin()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);

        var lastAdmin = User.Create("last@test.com", "Last", "Admin", "hash", UserRole.Admin);
        lastAdmin.Deactivate();
        context.Users.Add(lastAdmin);
        await context.SaveChangesAsync();

        var handler = new DeleteAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new DeleteAdminCommand(lastAdmin.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Cannot delete the last Admin.");
    }

    [Fact]
    public async Task Should_Throw_DomainException_When_Deleting_Self()
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
        admin.Deactivate();

        typeof(User)
            .GetProperty(nameof(User.Id))!
            .SetValue(admin, adminId);

        var secondAdmin = User.Create("other@test.com", "Other", "Admin", "hash", UserRole.Admin);

        context.Users.AddRange(admin, secondAdmin);
        await context.SaveChangesAsync();

        var handler = new DeleteAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new DeleteAdminCommand(adminId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("You cannot delete yourself.");
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

        var handler = new DeleteAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new DeleteAdminCommand(admin.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
