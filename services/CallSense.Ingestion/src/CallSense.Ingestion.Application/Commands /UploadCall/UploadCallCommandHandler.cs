using CallSense.Ingestion.Domain.Entities;
using CallSense.Ingestion.Domain.Interfaces;
using CallSense.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CallSense.Ingestion.Application.Commands.UploadCall;

public class UploadCallCommandHandler : IRequestHandler<UploadCallCommand, Result<Guid>>
{
    // Max file size: 500 MB — audio files can be large
    private const long MaxFileSizeBytes = 500 * 1024 * 1024;

    // Only accept these MIME types — reject anything else at this layer
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "audio/mpeg",   // .mp3
        "audio/wav",    // .wav
        "audio/mp4",    // .m4a
        "audio/ogg",    // .ogg
        "audio/webm"    // .webm
    ];

    private readonly ICallRecordRepository _repository;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<UploadCallCommandHandler> _logger;

    public UploadCallCommandHandler(
        ICallRecordRepository repository,
        IBlobStorageService blobStorage,
        ILogger<UploadCallCommandHandler> logger)
    {
        _repository = repository;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(UploadCallCommand command, CancellationToken cancellationToken)
    {
        // --- Step 1: Validate ---
        // FluentValidation runs BEFORE Handle() is called (via MediatR pipeline behaviour).
        // These checks here are domain-level guards — a second layer of safety.
        if (!AllowedContentTypes.Contains(command.ContentType))
            return Result<Guid>.Failure($"File type '{command.ContentType}' is not supported.");

        if (command.FileSizeBytes > MaxFileSizeBytes)
            return Result<Guid>.Failure($"File size exceeds the 500 MB limit.");

        // --- Step 2: Upload to Blob Storage ---
        // The file goes to Azure Blob Storage BEFORE we touch the database.
        // Why? Because blob upload can fail — if it does, we return early and
        // nothing is saved. Safe. No cleanup needed.
        string blobUrl;
        try
        {
            blobUrl = await _blobStorage.UploadAsync(
                tenantId: command.TenantId,
                fileName: command.FileName,
                content: command.FileContent,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blob upload failed for file {FileName}", command.FileName);
            return Result<Guid>.Failure("File upload failed. Please try again.");
        }

        // --- Step 3: Save CallRecord + OutboxMessage in one transaction ---
        // This is the Transactional Outbox Pattern.
        // Both writes succeed together or neither does — atomicity guaranteed.
        var callRecord = CallRecord.Create(
            tenantId: command.TenantId,
            originalFileName: command.FileName,
            blobUrl: blobUrl,
            fileSizeBytes: command.FileSizeBytes);

        await _repository.AddAsync(callRecord, cancellationToken);

        // Repository.AddAsync also writes the OutboxMessage in the same transaction.
        // See: CallRecordRepository — it writes both rows before SaveChangesAsync.

        _logger.LogInformation(
            "Call record {CallId} created for tenant {TenantId}",
            callRecord.Id, command.TenantId);

        return Result<Guid>.Success(callRecord.Id);
    }
}
