using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Enums;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles;

// BACKEND_AUDIT §26/§27 — the single role+tenant rule for a client's own data
// (measures / KPI / progress). Replaces the per-handler ValidatePermissions
// copies; fixes the CoachSelf 403 and the OwnerApp-on-measures 403.
public class ClientProfileAccessTests
{
    private static readonly Guid GymA = Guid.NewGuid();
    private static readonly Guid GymB = Guid.NewGuid();

    private static FakeCurrentUser User(UserRole role, Guid? gymId, Guid userId) =>
        new(gymId) { UserId = userId, Role = role, IsAuthenticated = true };

    [Fact]
    public void Owner_can_access_any_client_in_any_gym()
    {
        var owner = User(UserRole.OwnerApp, null, Guid.NewGuid());
        var profile = ClientProfile.Create(GymB, Guid.NewGuid(), Guid.NewGuid());

        owner.Invoking(u => u.EnsureCanAccess(profile)).Should().NotThrow();
    }

    [Fact]
    public void Admin_can_access_a_client_in_their_gym_but_not_another()
    {
        var admin = User(UserRole.Admin, GymA, Guid.NewGuid());

        admin.Invoking(u => u.EnsureCanAccess(ClientProfile.Create(GymA, Guid.NewGuid(), Guid.NewGuid())))
            .Should().NotThrow();
        admin.Invoking(u => u.EnsureCanAccess(ClientProfile.Create(GymB, Guid.NewGuid(), Guid.NewGuid())))
            .Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void Coach_can_access_an_assigned_client()
    {
        var coachUserId = Guid.NewGuid();
        var coach = User(UserRole.Coach, GymA, coachUserId);
        var assigned = ClientProfile.Create(GymA, Guid.NewGuid(), coachId: coachUserId);

        coach.Invoking(u => u.EnsureCanAccess(assigned)).Should().NotThrow();
    }

    [Fact]
    public void Coach_can_access_their_OWN_CoachSelf_profile()
    {
        var coachUserId = Guid.NewGuid();
        var coach = User(UserRole.Coach, GymA, coachUserId);
        var ownProfile = ClientProfile.CreateForCoachSelf(GymA, coachUserId); // CoachId == null

        coach.Invoking(u => u.EnsureCanAccess(ownProfile)).Should().NotThrow();
    }

    [Fact]
    public void Coach_cannot_access_an_unassigned_client_or_another_coachs_self_profile()
    {
        var coach = User(UserRole.Coach, GymA, Guid.NewGuid());

        coach.Invoking(u => u.EnsureCanAccess(ClientProfile.Create(GymA, Guid.NewGuid(), Guid.NewGuid())))
            .Should().Throw<ForbiddenException>();
        coach.Invoking(u => u.EnsureCanAccess(ClientProfile.CreateForCoachSelf(GymA, Guid.NewGuid())))
            .Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void Client_can_access_only_their_own_profile()
    {
        var clientUserId = Guid.NewGuid();
        var client = User(UserRole.Client, GymA, clientUserId);

        client.Invoking(u => u.EnsureCanAccess(ClientProfile.Create(GymA, clientUserId, Guid.NewGuid())))
            .Should().NotThrow();
        client.Invoking(u => u.EnsureCanAccess(ClientProfile.Create(GymA, Guid.NewGuid(), Guid.NewGuid())))
            .Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void Unauthenticated_is_rejected()
    {
        var anon = new FakeCurrentUser(GymA) { IsAuthenticated = false };

        anon.Invoking(u => u.EnsureCanAccess(ClientProfile.Create(GymA, Guid.NewGuid(), Guid.NewGuid())))
            .Should().Throw<UnauthorizedException>();
    }
}
