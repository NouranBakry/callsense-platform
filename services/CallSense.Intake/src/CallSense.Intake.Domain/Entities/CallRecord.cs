using CallSense.Intake.Domain.Enums;
using CallSense.SharedKernel;

namespace CallSense.Intake.Domain.Entities;

public class CallRecord : Entity
{
    public Guid TenantId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string BlobUrl { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public CallStatus Status { get; private set; }
    public string? TranscriptText { get; private set; }

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

    public void SetTranscript(string transcriptText)
    {
        TranscriptText = transcriptText;
        Status = CallStatus.Transcribed;
    }

    public void MarkAs(CallStatus status) => Status = status;
}
