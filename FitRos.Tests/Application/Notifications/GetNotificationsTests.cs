using FitRos.Application.Features.Notifications.GetNotifications;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Notifications;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.Notifications;

public class GetNotificationsTests
{
    [Fact]
    public async Task Should_Throw_When_User_Not_Authenticated()
    {
        var currentUser = new Mock<FitRos.Application.Abstractions.Security.ICurrentUser>();
        currentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var context = TestDbContextFactory.Create(currentUser.Object);
        var handler = new GetNotificationsHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GetNotificationsQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Return_Only_Current_User_Notifications()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = userId,
            Role = UserRole.Client,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var ownNotification = Notification.Create(
            userId, gymId, "My Title", "My message", NotificationType.RoutinePublished);

        var otherNotification = Notification.Create(
            otherUserId, gymId, "Other Title", "Other message", NotificationType.ClientAssigned);

        context.Notifications.Add(ownNotification);
        context.Notifications.Add(otherNotification);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetNotificationsHandler(context, fakeUser);

        var result = await handler.Handle(new GetNotificationsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Should_Return_Both_Read_And_Unread_Notifications()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = userId,
            Role = UserRole.Coach,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);

        var unread = Notification.Create(userId, gymId, "Unread", "Body", NotificationType.RoutineCreated);
        var read = Notification.Create(userId, gymId, "Read", "Body", NotificationType.GymCreated);
        read.MarkAsRead();

        context.Notifications.Add(unread);
        context.Notifications.Add(read);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetNotificationsHandler(context, fakeUser);

        var result = await handler.Handle(new GetNotificationsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_Return_Empty_When_No_Notifications()
    {
        var gymId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = userId,
            Role = UserRole.Client,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var handler = new GetNotificationsHandler(context, fakeUser);

        var result = await handler.Handle(new GetNotificationsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
