namespace FitRos.Domain.Entities.Auditing;

public sealed class AuditLogEntry
{
    public Guid Id { get; private set; }

    public string EventType { get; private set; } = null!;

    public string Data { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    private AuditLogEntry() { }

    private AuditLogEntry(Guid id, string eventType, string data)
    {
        Id = id;
        EventType = eventType;
        Data = data;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static AuditLogEntry Create(string eventType, string data)
        => new(Guid.NewGuid(), eventType, data);
}