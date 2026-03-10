using System.Text.Json;
using CallSense.Contracts.Messages;
using CallSense.Infrastructure.Shared.Outbox;
using CallSense.Intake.Domain.Entities;
using CallSense.Intake.Domain.Interfaces;

namespace CallSense.Intake.Infrastructure.Persistence.Repositories;

public class CallRecordRepository : ICallRecordRepository
{
    private readonly IntakeDbContext _db;

    public CallRecordRepository(IntakeDbContext db) => _db = db;

    public async Task AddAsync(CallRecord record, CancellationToken cancellationToken = default)
    {
        await _db.CallRecords.AddAsync(record, cancellationToken);

        var outboxMessage = new OutboxMessage
        {
            Type = nameof(CallTranscribed),
            Payload = JsonSerializer.Serialize(new CallTranscribed(
                CallId: record.Id,
                TenantId: record.TenantId,
                TranscriptText: record.TranscriptText!,
                BlobUrl: record.BlobUrl,
                TranscribedAt: record.CreatedAt))
        };

        await _db.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CallRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.CallRecords.FindAsync([id], cancellationToken);
}
