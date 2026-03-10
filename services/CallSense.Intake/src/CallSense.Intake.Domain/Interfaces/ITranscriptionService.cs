namespace CallSense.Intake.Domain.Interfaces;

public interface ITranscriptionService
{
    Task<string> TranscribeAsync(Stream audioStream, string fileName, CancellationToken cancellationToken = default);
}
