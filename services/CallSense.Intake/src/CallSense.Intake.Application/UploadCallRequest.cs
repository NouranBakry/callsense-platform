namespace CallSense.Intake.Application;

public record UploadCallRequest(
    Guid TenantId,
    string FileName,
    Stream FileContent,
    string ContentType,
    long FileSizeBytes);
