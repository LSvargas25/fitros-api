using FitRos.Application.Features.Users.UserManagement.ActivateUser;
using FitRos.Application.Features.Users.UserManagement.DeleteUser;
using FitRos.Application.Features.Users.UserManagement.UpdateUser;
using FitRos.Application.Features.Users.UserManagement.UserList;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using Xunit;

namespace FitRos.Tests.Application.Users;

// The UserManagement handlers used to let an Admin act on users in any gym
// (and GetUserById had no authorization at all). These assert the gym scope.
public class UserManagementTenantTests
{
    private static FakeCurrentUser CurrentUser(UserRole role, Guid gymId) => new(gymId)
    {
        UserId = Guid.NewGuid(),
        Role = role,
        IsAuthenticated = true
    };

    private static async Task<(FitRos.Infrastructure.Persistence.FitRosDbContext ctx, User target)>
        SeedTargetInGym(Guid targetGymId, UserRole role, bool deactivate = false)
    {
        var context = TestDbContextFactory.Create();
        var u = User.Create($"{Guid.NewGuid():N}@fitros.com", "First", "Last", "hash", role);
        u.AssignToGym(targetGymId);
        if (deactivate) u.Deactivate();
        context.Users.Add(u);
        await context.SaveChangesAsync(CancellationToken.None);
        return (context, u);
    }

    [Fact]
    public async Task GetUserById_denies_an_admin_from_another_gym()
    {
        var (ctx, target) = await SeedTargetInGym(Guid.NewGuid(), UserRole.Client);
        var handler = new GetUserByIdHandler(ctx, CurrentUser(UserRole.Admin, Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetUserByIdQuery(target.Id), CancellationToken.None));
    }

    [Fact]
    public async Task GetUserById_lets_a_client_read_only_themselves()
    {
        var (ctx, other) = await SeedTargetInGym(Guid.NewGuid(), UserRole.Client);
        var handler = new GetUserByIdHandler(ctx, CurrentUser(UserRole.Client, Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetUserByIdQuery(other.Id), CancellationToken.None));
    }

    [Fact]
    public async Task ActivateUser_denies_an_admin_from_another_gym()
    {
        var (ctx, target) = await SeedTargetInGym(Guid.NewGuid(), UserRole.Client, deactivate: true);
        var handler = new ActivateUserHandler(ctx, CurrentUser(UserRole.Admin, Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ActivateUserCommand(target.Id), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateUser_denies_an_admin_from_another_gym()
    {
        var (ctx, target) = await SeedTargetInGym(Guid.NewGuid(), UserRole.Client);
        var handler = new UpdateUserHandler(ctx, CurrentUser(UserRole.Admin, Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new UpdateUserCommand(target.Id, "X", "Y", null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task DeleteUser_denies_an_admin_from_another_gym()
    {
        var (ctx, target) = await SeedTargetInGym(Guid.NewGuid(), UserRole.Client, deactivate: true);
        var handler = new DeleteUserHandler(ctx, CurrentUser(UserRole.Admin, Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new DeleteUserCommand(target.Id), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteUser_denies_a_coach_from_another_gym()
    {
        var (ctx, target) = await SeedTargetInGym(Guid.NewGuid(), UserRole.Client, deactivate: true);
        var handler = new DeleteUserHandler(ctx, CurrentUser(UserRole.Coach, Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new DeleteUserCommand(target.Id), CancellationToken.None));
    }
}
