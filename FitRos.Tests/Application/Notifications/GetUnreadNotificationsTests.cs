using FitRos.Application.Features.Notifications.GetUnreadNotifications;
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

public class GetUnreadNotificationsTests
{
    [Fact]
    public async Task Should_Throw_When_User_Not_Authenticated()
    {
        var currentUser = new Mock<FitRos.Application.Abstractions.Security.ICurrentUser>();
        currentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var context = TestDbContextFactory.Create(currentUser.Object);
        var handler = new GetUnreadNotificationsHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GetUnreadNotificationsQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Return_Only_Unread_Notifications()
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

        var unread = Notification.Create(userId, gymId, "Unread", "Body", NotificationType.RoutineCreated);
        var read = Notification.Create(userId, gymId, "Read", "Body", NotificationType.GymCreated);
        read.MarkAsRead();

        context.Notifications.Add(unread);
        context.Notifications.Add(read);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetUnreadNotificationsHandler(context, fakeUser);

        var result = await handler.Handle(new GetUnreadNotificationsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Not_Return_Other_Users_Unread_Notifications()
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

        var otherUnread = Notification.Create(
            otherUserId, gymId, "Other", "Body", NotificationType.ClientAssigned);

        context.Notifications.Add(otherUnread);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetUnreadNotificationsHandler(context, fakeUser);

        var result = await handler.Handle(new GetUnreadNotificationsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
