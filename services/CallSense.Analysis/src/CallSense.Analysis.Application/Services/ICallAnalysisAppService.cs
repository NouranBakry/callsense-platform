using CallSense.Analysis.Domain.Entities;

namespace CallSense.Analysis.Application.Services;

public interface ICallAnalysisAppService
{
    Task ProcessAsync(Guid callId, Guid tenantId, string transcriptText, CancellationToken cancellationToken = default);
    Task<CallAnalysis?> GetByCallIdAsync(Guid callId, Guid tenantId, CancellationToken cancellationToken = default);
}
