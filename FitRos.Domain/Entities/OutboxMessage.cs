namespace FitRos.Domain.Entities.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }

    public string Type { get; private set; } = null!;

    public string Payload { get; private set; } = null!;

    public DateTime OccurredOnUtc { get; private set; }

    public DateTime? ProcessedOnUtc { get; private set; }

    public string? Error { get; private set; }

    private OutboxMessage() { }

    private OutboxMessage(Guid id, string type, string payload, DateTime occurredOnUtc)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredOnUtc = occurredOnUtc;
    }

    public static OutboxMessage Create(string type, string payload, DateTime occurredOnUtc)
        => new(Guid.NewGuid(), type, payload, occurredOnUtc);

    public void MarkProcessed()
        => ProcessedOnUtc = DateTime.UtcNow;

    public void MarkFailed(string error)
        => Error = error;
}