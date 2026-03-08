using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;

namespace FitRos.Domain.Entities.Notifications;

public sealed class Notification : ITenantEntity
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid? GymId { get; private set; }

    Guid? ITenantEntity.GymId
    {
        get => GymId;
        set => GymId = value;
    }

    public string Title { get; private set; } = null!;

    public string Message { get; private set; } = null!;

    public NotificationType Type { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public bool IsRead { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private Notification() { }

    private Notification(
        Guid id,
        Guid userId,
        Guid? gymId,
        string title,
        string message,
        NotificationType type,
        Guid? referenceId)
    {
        Id = id;
        UserId = userId;
        GymId = gymId;
        Title = title;
        Message = message;
        Type = type;
        ReferenceId = referenceId;
        IsRead = false;
        CreatedAt = DateTime.UtcNow;
    }

    public static Notification Create(
        Guid userId,
        Guid? gymId,
        string title,
        string message,
        NotificationType type,
        Guid? referenceId = null)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Notification title cannot be empty.");

        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("Notification message cannot be empty.");

        return new Notification(
            Guid.NewGuid(),
            userId,
            gymId,
            title.Trim(),
            message.Trim(),
            type,
            referenceId);
    }

    public void MarkAsRead()
    {
        if (IsRead)
            throw new DomainException("Notification is already marked as read.");

        IsRead = true;
    }
}
