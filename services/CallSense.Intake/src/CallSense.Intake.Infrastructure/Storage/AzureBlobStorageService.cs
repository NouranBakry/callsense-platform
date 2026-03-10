using Azure.Storage.Blobs;
using CallSense.Intake.Domain.Interfaces;

namespace CallSense.Intake.Infrastructure.Storage;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobStorageService(BlobServiceClient blobServiceClient)
        => _blobServiceClient = blobServiceClient;

    public async Task<string> UploadAsync(Guid tenantId, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var containerName = $"tenant-{tenantId:N}";
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}-{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(content, overwrite: true, cancellationToken: cancellationToken);

        return blobClient.Uri.ToString();
    }
}
