using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.Gyms.CreateGym;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.Gyms;

public class CreateGymHandlerTests
{
    [Fact]
    public async Task Should_Create_Gym_And_Admin()
    {
        var context = TestDbContextFactory.Create(
            role: UserRole.OwnerApp);

        var passwordHasher = new Mock<IPasswordHasher>();

        passwordHasher
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed-password");

        var currentUser = new Mock<ICurrentUser>();

        currentUser.Setup(x => x.UserId)
            .Returns(Guid.NewGuid());

        currentUser.Setup(x => x.Role)
            .Returns(UserRole.OwnerApp);

        var handler = new CreateGymHandler(
            context,
            passwordHasher.Object,
            currentUser.Object);

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

        var gym = context.Gyms.First();

        Assert.Equal("FitRos Gym", gym.Name);

        var admin = context.Users.First(x => x.Role == UserRole.Admin);

        Assert.Equal(gym.Id, admin.GymId);

        var audit = context.AuditLogEntries.First();

        Assert.Equal("GymCreated", audit.EventType);

        var outbox = context.OutboxMessages.First();

        Assert.Equal("GymCreated", outbox.Type);
    }
}