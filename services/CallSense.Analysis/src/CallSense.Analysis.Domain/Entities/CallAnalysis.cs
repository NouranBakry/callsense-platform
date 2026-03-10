using CallSense.Analysis.Domain.Enums;
using CallSense.SharedKernel;

namespace CallSense.Analysis.Domain.Entities;

public class CallAnalysis : Entity
{
    public Guid CallId { get; private set; }
    public Guid TenantId { get; private set; }
    public int OverallScore { get; private set; }
    public string? Report { get; private set; }
    public string? Strengths { get; private set; }
    public string? Improvements { get; private set; }
    public AnalysisStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    private CallAnalysis() { }

    public static CallAnalysis Create(Guid callId, Guid tenantId) => new()
    {
        CallId = callId,
        TenantId = tenantId,
        Status = AnalysisStatus.Pending
    };

    public void SetResult(int score, string report, string strengths, string improvements)
    {
        OverallScore = score;
        Report = report;
        Strengths = strengths;
        Improvements = improvements;
        Status = AnalysisStatus.Completed;
    }

    public void MarkFailed(string error)
    {
        ErrorMessage = error;
        Status = AnalysisStatus.Failed;
    }
}
