namespace CallSense.Contracts.Messages;

public record CallTranscribed(
    Guid CallId,
    Guid TenantId,
    string TranscriptText,
    string BlobUrl,
    DateTimeOffset TranscribedAt);
