using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.Gyms.CreateGym;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.Gyms;

public class CreateGymHandlerTests
{
    [Fact]
    public async Task Should_Create_Gym_And_Admin()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var passwordHasher = new Mock<IPasswordHasher>();

        passwordHasher
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed-password");

        var handler = new CreateGymHandler(
            context,
            passwordHasher.Object,
            fakeUser);

        var command = new CreateGymCommand(
            "FitRos Gym",
            "San Jose",
            "8888-8888",
            null,
            "admin@gym.com",
            "Admin",
            "Gym",
            "Password123");

        var result = await handler.Handle(command, CancellationToken.None);

        var gym = context.Gyms
            .IgnoreQueryFilters()
            .First();

        Assert.Equal("FitRos Gym", gym.Name);

        var admin = context.Users
            .IgnoreQueryFilters()
            .First(x => x.Role == UserRole.Admin);

        Assert.Equal(gym.Id, admin.GymId);

        var audit = context.AuditLogEntries
            .IgnoreQueryFilters()
            .First();

        Assert.Equal("GymCreated", audit.EventType);

        var outbox = context.OutboxMessages
            .IgnoreQueryFilters()
            .First();

        Assert.Equal("GymCreated", outbox.Type);
    }
}