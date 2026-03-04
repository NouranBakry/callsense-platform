using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CallSense.Ingestion.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CallSense.Ingestion.Infrastructure.Storage;

// Implements IBlobStorageService (defined in Domain) using the Azure Storage Blobs SDK.
// In local dev this talks to Azurite (the local Azure Storage emulator).
// In production it talks to real Azure Blob Storage — same code, different connection string.
public class AzureBlobStorageService : IBlobStorageService
{
    // Container names must be lowercase, 3–63 chars, only letters/digits/hyphens.
    // We use one container per tenant: "tenant-{tenantId}" gives natural data isolation.
    // Tenant A can never read Tenant B's blobs because they're in separate containers.
    private const string ContainerPrefix = "tenant-";

    // BlobServiceClient is thread-safe and heavyweight (manages connection pools).
    // Register it as Singleton in DI — one instance shared across all requests.
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(BlobServiceClient blobServiceClient, ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Guid tenantId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        // Container name: "tenant-{tenantId without hyphens, lowercase}"
        // :N format = no hyphens. Lower-case required by Azure container naming rules.
        var containerName = $"{ContainerPrefix}{tenantId:N}".ToLowerInvariant();

        // GetBlobContainerClient is a local operation — no network call yet.
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        // Create the container if it doesn't already exist.
        // PublicAccessType.None = private container — blobs are not publicly readable.
        // This is idempotent and safe to call every request; it's a no-op if the
        // container exists. Production optimisation: cache existence in memory.
        await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        // Prefix the blob name with a timestamp + random GUID to prevent collisions
        // when the same file is uploaded twice (or two files share a name).
        // Pattern: "20240301120000-{guid}-originalfilename.mp3"
        var blobName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}-{fileName}";

        var blobClient = containerClient.GetBlobClient(blobName);

        // Upload the stream. overwrite: false ensures we never silently stomp an
        // existing blob — if the generated name somehow collides (extremely unlikely),
        // the SDK throws instead of corrupting data.
        await blobClient.UploadAsync(content, overwrite: false, cancellationToken);

        _logger.LogDebug(
            "Uploaded blob {BlobName} to container {Container}",
            blobName, containerName);

        // Return the full URI of the uploaded blob.
        // This URL is stored in CallRecord.BlobUrl and later passed to
        // Transcription service via the CallRecordingUploaded outbox message.
        return blobClient.Uri.ToString();
    }
}
