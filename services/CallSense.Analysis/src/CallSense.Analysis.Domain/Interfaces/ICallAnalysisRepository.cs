using CallSense.Analysis.Domain.Entities;

namespace CallSense.Analysis.Domain.Interfaces;

public interface ICallAnalysisRepository
{
    Task AddAsync(CallAnalysis analysis, CancellationToken cancellationToken = default);
    Task<CallAnalysis?> GetByCallIdAsync(Guid callId, CancellationToken cancellationToken = default);
}
