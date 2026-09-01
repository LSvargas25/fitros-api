using FitRos.Application.Features.Gyms.CreateGym;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Gyms;

public class CreateGymHandlerTests
{
    [Fact]
    public async Task OwnerApp_Creates_Gym_Without_Admin()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var handler = new CreateGymHandler(context, fakeUser);

        var command = new CreateGymCommand("FitRos Gym", "San Jose", "8888-8888");

        var gymId = await handler.Handle(command, CancellationToken.None);

        gymId.Should().NotBeEmpty();

        var gym = await context.Gyms.IgnoreQueryFilters().FirstAsync(g => g.Id == gymId);
        gym.Name.Should().Be("FitRos Gym");

        var audit = await context.AuditLogEntries.IgnoreQueryFilters().FirstAsync();
        audit.EventType.Should().Be("GymCreated");

        var outbox = await context.OutboxMessages.IgnoreQueryFilters().FirstAsync();
        outbox.Type.Should().Be("GymCreated");
    }

    [Fact]
    public async Task Creates_Gym_With_No_Users_In_Db()
    {
        var fakeUser = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var handler = new CreateGymHandler(context, fakeUser);

        var command = new CreateGymCommand("Solo Gym", "Heredia", "7777-7777");

        var gymId = await handler.Handle(command, CancellationToken.None);

        var admins = context.Users.IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.Admin).ToList();

        admins.Should().BeEmpty();
    }
}
