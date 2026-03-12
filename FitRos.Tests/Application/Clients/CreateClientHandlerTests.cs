using FitRos.Application.Features.Users.Client.CreateClient;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Clients;

public class CreateClientHandlerTests
{
    [Fact]
    public async Task OwnerApp_Can_Create_Client_With_Explicit_GymId()
    {
        var targetGymId = Guid.NewGuid();

        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var hasher = new FakePasswordHasher();
        var handler = new CreateClientHandler(context, hasher, fakeOwner);

        var command = new CreateClientCommand(
            "client@test.com", "Jane", "Doe", "Password123",
            GymId: targetGymId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Email.Should().Be("client@test.com");
        result.Role.Should().Be((int)UserRole.Client);

        var userInDb = await context.Users
            .IgnoreQueryFilters()
            .FirstAsync(u => u.Id == result.Id);

        userInDb.GymId.Should().Be(targetGymId);
    }

    [Fact]
    public async Task OwnerApp_Cannot_Create_Client_Without_GymId()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };
        var context = TestDbContextFactory.Create(fakeOwner);
        var hasher = new FakePasswordHasher();
        var handler = new CreateClientHandler(context, hasher, fakeOwner);

        var command = new CreateClientCommand(
            "client@test.com", "Jane", "Doe", "Password123"); // sin GymId

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("GymId is required when OwnerApp creates a client.");
    }
    [Fact]
    public async Task OwnerApp_Can_Create_Client_With_GymId()
    {
        var gymId = Guid.NewGuid();
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };
        var context = TestDbContextFactory.Create(fakeOwner);
        var hasher = new FakePasswordHasher();
        var handler = new CreateClientHandler(context, hasher, fakeOwner);

        var command = new CreateClientCommand(
            "client@test.com", "Jane", "Doe", "Password123", gymId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Role.Should().Be((int)UserRole.Client);

        var profile = await context.ClientProfiles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.UserId == result.Id);

        profile.Should().NotBeNull();
        profile!.GymId.Should().Be(gymId);
    }

    [Fact]
    public async Task Admin_Creates_Client_In_Own_Gym()
    {
        var gymId = Guid.NewGuid();

        var fakeAdmin = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeAdmin);
        var hasher = new FakePasswordHasher();
        var handler = new CreateClientHandler(context, hasher, fakeAdmin);

        var command = new CreateClientCommand(
            "client2@test.com", "John", "Smith", "Password123");

        var result = await handler.Handle(command, CancellationToken.None);

        var userInDb = await context.Users
            .IgnoreQueryFilters()
            .FirstAsync(u => u.Id == result.Id);

        userInDb.GymId.Should().Be(gymId);
        userInDb.Role.Should().Be(UserRole.Client);
    }

    [Fact]
    public async Task Coach_Creates_Client_Inheriting_GymId_And_CoachId()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var fakeCoach = new FakeCurrentUser(gymId)
        {
            UserId = coachId,
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeCoach);
        var hasher = new FakePasswordHasher();
        var handler = new CreateClientHandler(context, hasher, fakeCoach);

        var command = new CreateClientCommand(
            "client3@test.com", "Mark", "Client", "Password123");

        var result = await handler.Handle(command, CancellationToken.None);

        var userInDb = await context.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.Id == result.Id);
        userInDb.GymId.Should().Be(gymId);
        userInDb.Role.Should().Be(UserRole.Client);

        var profile = await context.ClientProfiles.IgnoreQueryFilters()
            .FirstAsync(p => p.UserId == result.Id);
        profile.CoachId.Should().Be(coachId);
    }
}
