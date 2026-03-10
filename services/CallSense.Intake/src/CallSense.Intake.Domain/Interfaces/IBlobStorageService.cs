namespace CallSense.Intake.Domain.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(Guid tenantId, string fileName, Stream content, CancellationToken cancellationToken = default);
}
