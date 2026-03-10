using CallSense.Intake.Domain.Entities;
using CallSense.Intake.Domain.Interfaces;
using CallSense.SharedKernel;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CallSense.Intake.Application.Services;

public class CallService : ICallService
{
    private readonly ICallRecordRepository _repository;
    private readonly IBlobStorageService _blobStorage;
    private readonly ITranscriptionService _transcription;
    private readonly IValidator<UploadCallRequest> _validator;
    private readonly ILogger<CallService> _logger;

    public CallService(
        ICallRecordRepository repository,
        IBlobStorageService blobStorage,
        ITranscriptionService transcription,
        IValidator<UploadCallRequest> validator,
        ILogger<CallService> logger)
    {
        _repository = repository;
        _blobStorage = blobStorage;
        _transcription = transcription;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid>> UploadAsync(UploadCallRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<Guid>.Failure(errors);
        }

        string transcriptText;
        try
        {
            transcriptText = await _transcription.TranscribeAsync(
                request.FileContent, request.FileName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed for {FileName}", request.FileName);
            return Result<Guid>.Failure("Transcription failed. Please try again.");
        }

        request.FileContent.Seek(0, SeekOrigin.Begin);

        string blobUrl;
        try
        {
            blobUrl = await _blobStorage.UploadAsync(
                request.TenantId, request.FileName, request.FileContent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blob upload failed for {FileName}", request.FileName);
            return Result<Guid>.Failure("File upload failed. Please try again.");
        }

        var callRecord = CallRecord.Create(request.TenantId, request.FileName, blobUrl, request.FileSizeBytes);
        callRecord.SetTranscript(transcriptText);
        await _repository.AddAsync(callRecord, cancellationToken);

        _logger.LogInformation("Call {CallId} transcribed and saved", callRecord.Id);
        return Result<Guid>.Success(callRecord.Id);
    }

    public async Task<CallRecord?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var record = await _repository.GetByIdAsync(id, cancellationToken);
        if (record is null || record.TenantId != tenantId) return null;
        return record;
    }
}
