using FitRos.Application.Features.ClientProfiles.ActivateClientInGym;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles;

public class ActivateClientInGymHandlerTests
{
    [Fact]
    public async Task Activates_Inactive_Client_And_Updates_GymId()
    {
        var gymId = Guid.NewGuid();
        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var gym = Gym.Create("Target Gym", "San Jose", "1111-1111");
        context.Gyms.Add(gym);
        await context.SaveChangesAsync(CancellationToken.None);

        var clientUserId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var oldGymId = Guid.NewGuid();
        var profile = ClientProfile.Create(oldGymId, clientUserId, coachId);
        profile.Deactivate();
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync(CancellationToken.None);

        var targetGymId = context.Gyms.First().Id;
        var handler = new ActivateClientInGymHandler(context, fakeUser);

        await handler.Handle(new ActivateClientInGymCommand(profile.Id, targetGymId), CancellationToken.None);

        var updatedProfile = await context.ClientProfiles
            .IgnoreQueryFilters()
            .FirstAsync(cp => cp.Id == profile.Id);

        updatedProfile.Status.Should().Be(ClientStatus.Active);
        ((ITenantEntity)updatedProfile).GymId.Should().Be(targetGymId);
    }

    [Fact]
    public async Task Throws_When_Profile_Not_Found()
    {
        var gymId = Guid.NewGuid();
        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var gym = Gym.Create("Some Gym", "Alajuela", "2222-2222");
        context.Gyms.Add(gym);
        await context.SaveChangesAsync(CancellationToken.None);

        var gymActualId = context.Gyms.First().Id;
        var handler = new ActivateClientInGymHandler(context, fakeUser);

        var act = async () => await handler.Handle(
            new ActivateClientInGymCommand(Guid.NewGuid(), gymActualId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_When_Activating_Already_Active_Client()
    {
        var gymId = Guid.NewGuid();
        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var gym = Gym.Create("Some Gym", "Cartago", "3333-3333");
        context.Gyms.Add(gym);
        await context.SaveChangesAsync(CancellationToken.None);

        var gymActualId = context.Gyms.First().Id;

        var clientUserId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var profile = ClientProfile.Create(gymActualId, clientUserId, coachId);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ActivateClientInGymHandler(context, fakeUser);

        var act = async () => await handler.Handle(
            new ActivateClientInGymCommand(profile.Id, gymActualId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Only inactive clients can be activated.");
    }
}
