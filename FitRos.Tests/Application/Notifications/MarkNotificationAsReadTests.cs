using FitRos.Application.Features.Notifications.MarkNotificationAsRead;
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

public class MarkNotificationAsReadTests
{
    [Fact]
    public async Task Should_Throw_When_User_Not_Authenticated()
    {
        var currentUser = new Mock<FitRos.Application.Abstractions.Security.ICurrentUser>();
        currentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var context = TestDbContextFactory.Create(currentUser.Object);
        var handler = new MarkNotificationAsReadHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(
                new MarkNotificationAsReadCommand(Guid.NewGuid()),
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
        var handler = new MarkNotificationAsReadHandler(context, fakeUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new MarkNotificationAsReadCommand(Guid.NewGuid()),
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
            otherUserId, gymId, "Title", "Message", NotificationType.RoutinePublished);

        context.Notifications.Add(notification);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkNotificationAsReadHandler(context, fakeUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new MarkNotificationAsReadCommand(notification.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Mark_Notification_As_Read_When_Valid()
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
            userId, gymId, "Title", "Message", NotificationType.WorkoutScheduled);

        context.Notifications.Add(notification);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkNotificationAsReadHandler(context, fakeUser);

        await handler.Handle(
            new MarkNotificationAsReadCommand(notification.Id),
            CancellationToken.None);

        var updated = await context.Notifications.SingleAsync(n => n.Id == notification.Id);

        updated.IsRead.Should().BeTrue();
    }
}
