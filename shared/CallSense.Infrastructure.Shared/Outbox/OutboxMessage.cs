namespace CallSense.Infrastructure.Shared.Outbox;

/// <summary>
/// Persisted in the same DB transaction as the business record.
/// OutboxPublisherWorker reads these rows and publishes them to RabbitMQ,
/// guaranteeing at-least-once message delivery.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The full message type name, e.g. "CallRecordingUploaded"</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>JSON-serialized message payload</summary>
    public string Payload { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Null = not yet processed. Set by OutboxPublisherWorker after successful publish.</summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>Last error message if publishing failed. Informational only.</summary>
    public string? Error { get; set; }
}
