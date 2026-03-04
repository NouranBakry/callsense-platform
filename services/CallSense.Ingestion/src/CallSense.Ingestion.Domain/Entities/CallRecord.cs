using CallSense.Ingestion.Domain.Enums;
using CallSense.SharedKernel;

namespace CallSense.Ingestion.Domain.Entities;

public class CallRecord : Entity
{
    public Guid TenantId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string BlobUrl { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public CallStatus Status { get; private set; }

    // Required by EF Core
    private CallRecord() { }

    public static CallRecord Create(Guid tenantId, string originalFileName, string blobUrl, long fileSizeBytes)
    {
        return new CallRecord
        {
            TenantId = tenantId,
            OriginalFileName = originalFileName,
            BlobUrl = blobUrl,
            FileSizeBytes = fileSizeBytes,
            Status = CallStatus.Uploaded
        };
    }

    public void MarkAs(CallStatus status) => Status = status;
}
