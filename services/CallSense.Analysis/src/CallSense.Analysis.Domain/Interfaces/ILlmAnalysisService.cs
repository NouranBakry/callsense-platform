namespace CallSense.Analysis.Domain.Interfaces;

public interface ILlmAnalysisService
{
    Task<LlmAnalysisResult> AnalyzeTranscriptAsync(string transcriptText, CancellationToken cancellationToken = default);
}

public record LlmAnalysisResult(int OverallScore, string Report, string[] Strengths, string[] Improvements);
