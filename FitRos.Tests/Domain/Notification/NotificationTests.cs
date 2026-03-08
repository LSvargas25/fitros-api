using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Notifications;
using Xunit;

namespace FitRos.Tests.Domain.Notifications;

public class NotificationTests
{
    [Fact]
    public void Create_Should_Return_Notification_With_IsRead_False()
    {
        var notification = Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Title",
            "Test message body.",
            NotificationType.RoutinePublished);

        Assert.False(notification.IsRead);
    }

    [Fact]
    public void Create_Should_Set_CreatedAt_To_UtcNow()
    {
        var before = DateTime.UtcNow;

        var notification = Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Title",
            "Message",
            NotificationType.ClientAssigned);

        Assert.True(notification.CreatedAt >= before);
    }

    [Fact]
    public void Create_Should_Set_ReferenceId_When_Provided()
    {
        var referenceId = Guid.NewGuid();

        var notification = Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Title",
            "Message",
            NotificationType.RoutineAssigned,
            referenceId);

        Assert.Equal(referenceId, notification.ReferenceId);
    }

    [Fact]
    public void Create_Should_Throw_When_Title_Is_Empty()
    {
        Assert.Throws<DomainException>(() =>
            Notification.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                string.Empty,
                "Message",
                NotificationType.GymCreated));
    }

    [Fact]
    public void Create_Should_Throw_When_Message_Is_Empty()
    {
        Assert.Throws<DomainException>(() =>
            Notification.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Title",
                string.Empty,
                NotificationType.GymCreated));
    }

    [Fact]
    public void Create_Should_Throw_When_UserId_Is_Empty()
    {
        Assert.Throws<DomainException>(() =>
            Notification.Create(
                Guid.Empty,
                Guid.NewGuid(),
                "Title",
                "Message",
                NotificationType.GymCreated));
    }

    [Fact]
    public void MarkAsRead_Should_Set_IsRead_To_True()
    {
        var notification = Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Title",
            "Message",
            NotificationType.WorkoutScheduled);

        notification.MarkAsRead();

        Assert.True(notification.IsRead);
    }

    [Fact]
    public void MarkAsRead_Should_Throw_When_Already_Read()
    {
        var notification = Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Title",
            "Message",
            NotificationType.RoutineCreated);

        notification.MarkAsRead();

        Assert.Throws<DomainException>(() => notification.MarkAsRead());
    }
}
