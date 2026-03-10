using CallSense.Intake.Domain.Entities;

namespace CallSense.Intake.Domain.Interfaces;

public interface ICallRecordRepository
{
    Task AddAsync(CallRecord record, CancellationToken cancellationToken = default);
    Task<CallRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
