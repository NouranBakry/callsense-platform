using System.Text.Json;
using CallSense.Contracts.Messages;
using CallSense.Infrastructure.Shared.Outbox;
using CallSense.Ingestion.Domain.Entities;
using CallSense.Ingestion.Domain.Interfaces;

namespace CallSense.Ingestion.Infrastructure.Persistence.Repositories;

// Implements ICallRecordRepository (defined in Domain) using EF Core.
// The Domain interface doesn't know EF exists — this class is the bridge.
// Registered as Scoped in DI (one instance per HTTP request / per DI scope).
public class CallRecordRepository : ICallRecordRepository
{
    // IngestionDbContext is the EF unit-of-work for this service.
    // It's also Scoped, so the lifetime matches — safe to inject directly.
    private readonly IngestionDbContext _db;

    public CallRecordRepository(IngestionDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(CallRecord record, CancellationToken cancellationToken = default)
    {
        // Stage the CallRecord for insertion. EF doesn't write to the DB yet —
        // it tracks the entity in memory as "Added".
        await _db.CallRecords.AddAsync(record, cancellationToken);

        // Build the outbox message from the domain entity's data.
        // nameof(CallRecordingUploaded) = "CallRecordingUploaded" — the Type
        // string the OutboxPublisherWorker uses to deserialize the payload.
        var outboxMessage = new OutboxMessage
        {
            Type = nameof(CallRecordingUploaded),

            // Serialize the message contract to JSON. This is what gets
            // stored in the Payload column and later published to RabbitMQ.
            // Using the Contracts record here means Transcription will receive
            // exactly the same type when it deserializes.
            Payload = JsonSerializer.Serialize(new CallRecordingUploaded(
                CallId: record.Id,
                TenantId: record.TenantId,
                BlobUrl: record.BlobUrl,
                OriginalFileName: record.OriginalFileName,
                FileSizeBytes: record.FileSizeBytes,
                UploadedAt: record.CreatedAt))
        };

        // Stage the OutboxMessage for insertion in the same tracked context.
        await _db.OutboxMessages.AddAsync(outboxMessage, cancellationToken);

        // THIS is the atomic commit — both CallRecord and OutboxMessage are written
        // in a single PostgreSQL transaction. If either INSERT fails, both are
        // rolled back. This is what guarantees the Outbox Pattern's at-least-once
        // delivery: if the app crashes after this commit, the message is safe in the
        // DB and the OutboxPublisherWorker will pick it up next cycle.
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CallRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // FindAsync uses the primary key cache first (avoids a DB round-trip
        // if the entity was already loaded in this scope), then hits the DB.
        // [id] is the params array syntax for composite keys — works for single keys too.
        return await _db.CallRecords.FindAsync([id], cancellationToken);
    }
}
