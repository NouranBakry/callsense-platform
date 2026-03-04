namespace CallSense.Contracts.Messages;

/// <summary>
/// Published by Ingestion when an audio file has been successfully uploaded.
/// Consumed by Transcription to begin speech-to-text processing.
/// </summary>
public record CallRecordingUploaded(
    Guid CallId,
    Guid TenantId,
    string BlobUrl,
    string OriginalFileName,
    long FileSizeBytes,
    DateTimeOffset UploadedAt);
