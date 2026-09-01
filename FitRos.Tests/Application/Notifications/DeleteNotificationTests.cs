using FitRos.Application.Features.Notifications.DeleteNotification;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Notifications;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.Notifications;

public class DeleteNotificationTests
{
    [Fact]
    public async Task Should_Throw_When_User_Not_Authenticated()
    {
        var currentUser = new Mock<FitRos.Application.Abstractions.Security.ICurrentUser>();
        currentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var context = TestDbContextFactory.Create(currentUser.Object);
        var handler = new DeleteNotificationHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(
                new DeleteNotificationCommand(Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Notification_Not_Found()
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
        var handler = new DeleteNotificationHandler(context, fakeUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new DeleteNotificationCommand(Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Notification_Belongs_To_Another_User()
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

        var notification = Notification.Create(
            otherUserId, gymId, "Title", "Message", NotificationType.ClientAssigned);

        context.Notifications.Add(notification);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteNotificationHandler(context, fakeUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new DeleteNotificationCommand(notification.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Delete_Notification_When_Valid()
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

        var notification = Notification.Create(
            userId, gymId, "Title", "Message", NotificationType.RoutineCreated);

        context.Notifications.Add(notification);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteNotificationHandler(context, fakeUser);

        await handler.Handle(
            new DeleteNotificationCommand(notification.Id),
            CancellationToken.None);

        var exists = await context.Notifications.AnyAsync(n => n.Id == notification.Id);

        exists.Should().BeFalse();
    }
}
