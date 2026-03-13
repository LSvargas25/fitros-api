using FitRos.Application.Features.Users.Admin.UpdateAdmin;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Admins;

public class UpdateAdminHandlerTests
{
    [Fact]
    public async Task OwnerApp_Can_Update_Admin_Name()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var admin = User.Create("admin@test.com", "Alice", "Old", "hash", UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var handler = new UpdateAdminHandler(context, fakeOwner);
        var result = await handler.Handle(
            new UpdateAdminCommand(admin.Id, "Alice", "New", null), CancellationToken.None);

        result.LastName.Should().Be("New");

        var inDb = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == admin.Id);
        inDb.LastName.Should().Be("New");
    }

    [Fact]
    public async Task OwnerApp_Can_Update_Admin_Email()
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

        var handler = new UpdateAdminHandler(context, fakeOwner);
        var result = await handler.Handle(
            new UpdateAdminCommand(admin.Id, null, null, "newemail@test.com"), CancellationToken.None);

        result.Email.Should().Be("newemail@test.com");
    }

    [Fact]
    public async Task Throws_NotFoundException_When_Admin_Not_Found()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var handler = new UpdateAdminHandler(context, fakeOwner);

        var act = async () =>
            await handler.Handle(new UpdateAdminCommand(Guid.NewGuid(), "X", "Y", null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFoundException_When_Id_Belongs_To_Non_Admin()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var gymId = Guid.NewGuid();
        var coach = User.CreateForGym(gymId, "coach@test.com", "Coach", "User", "hash", UserRole.Coach);
        context.Users.Add(coach);
        await context.SaveChangesAsync();

        var handler = new UpdateAdminHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new UpdateAdminCommand(coach.Id, "X", "Y", null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_When_Email_Already_In_Use()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var admin1 = User.Create("taken@test.com", "Taken", "Email", "hash", UserRole.Admin);
        var admin2 = User.Create("admin2@test.com", "Admin", "Two", "hash", UserRole.Admin);
        context.Users.AddRange(admin1, admin2);
        await context.SaveChangesAsync();

        var handler = new UpdateAdminHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new UpdateAdminCommand(admin2.Id, null, null, "taken@test.com"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Email already in use.");
    }

    [Fact]
    public async Task Throws_UnauthorizedException_When_Not_Authenticated()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = false
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var admin = User.Create("admin@test.com", "Alice", "Admin", "hash", UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var handler = new UpdateAdminHandler(context, fakeOwner);
        var act = async () =>
            await handler.Handle(new UpdateAdminCommand(admin.Id, "Alice", "Updated", null), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
