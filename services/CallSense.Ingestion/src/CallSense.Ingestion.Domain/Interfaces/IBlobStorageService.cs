namespace CallSense.Ingestion.Domain.Interfaces;

// Defined in Domain so the Application layer (UploadCallCommandHandler)
// can depend on it without knowing about Azure, S3, or any other storage provider.
// The concrete AzureBlobStorageService lives in Infrastructure.
public interface IBlobStorageService
{
    // Uploads the stream to blob storage under the given tenant's container.
    // Returns the public URL of the uploaded blob — this URL is stored in CallRecord.BlobUrl
    // and passed to Transcription via the outbox message.
    Task<string> UploadAsync(
        Guid tenantId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);
}
