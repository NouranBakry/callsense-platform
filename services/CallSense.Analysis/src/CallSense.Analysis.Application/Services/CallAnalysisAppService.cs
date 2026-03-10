using System.Text.Json;
using CallSense.Analysis.Domain.Entities;
using CallSense.Analysis.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CallSense.Analysis.Application.Services;

public class CallAnalysisAppService : ICallAnalysisAppService
{
    private readonly ICallAnalysisRepository _repository;
    private readonly ILlmAnalysisService _llm;
    private readonly ILogger<CallAnalysisAppService> _logger;

    public CallAnalysisAppService(ICallAnalysisRepository repository, ILlmAnalysisService llm, ILogger<CallAnalysisAppService> logger)
    {
        _repository = repository;
        _llm = llm;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid callId, Guid tenantId, string transcriptText, CancellationToken cancellationToken = default)
    {
        var analysis = CallAnalysis.Create(callId, tenantId);
        try
        {
            _logger.LogInformation("Analysing call {CallId}", callId);
            var result = await _llm.AnalyzeTranscriptAsync(transcriptText, cancellationToken);
            analysis.SetResult(
                result.OverallScore,
                result.Report,
                JsonSerializer.Serialize(result.Strengths),
                JsonSerializer.Serialize(result.Improvements));
            _logger.LogInformation("Call {CallId} scored {Score}/10", callId, result.OverallScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed for call {CallId}", callId);
            analysis.MarkFailed(ex.Message);
        }
        await _repository.AddAsync(analysis, cancellationToken);
    }

    public async Task<CallAnalysis?> GetByCallIdAsync(Guid callId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var analysis = await _repository.GetByCallIdAsync(callId, cancellationToken);
        if (analysis is null || analysis.TenantId != tenantId) return null;
        return analysis;
    }
}
