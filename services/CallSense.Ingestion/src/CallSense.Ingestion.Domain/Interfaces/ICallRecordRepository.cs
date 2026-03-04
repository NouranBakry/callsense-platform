using CallSense.Ingestion.Domain.Entities;

namespace CallSense.Ingestion.Domain.Interfaces;

// This interface lives in Domain — the innermost layer.
// It defines what the repository CAN do, not HOW it does it.
// The actual EF Core implementation lives in Infrastructure,
// keeping SQL and EF out of the Domain entirely.
public interface ICallRecordRepository
{
    // Persist a new CallRecord AND write the outbox message atomically.
    // CancellationToken on every async method is non-negotiable — it lets
    // the runtime cancel the DB call when the HTTP request is aborted.
    Task AddAsync(CallRecord record, CancellationToken cancellationToken = default);

    // Returns null if not found — callers must handle the null case.
    // Nullable reference type (CallRecord?) makes that contract explicit.
    Task<CallRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
