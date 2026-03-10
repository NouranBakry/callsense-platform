using CallSense.Analysis.Domain.Entities;
using CallSense.Analysis.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CallSense.Analysis.Infrastructure.Persistence.Repositories;

public class CallAnalysisRepository : ICallAnalysisRepository
{
    private readonly AnalysisDbContext _db;
    public CallAnalysisRepository(AnalysisDbContext db) => _db = db;

    public async Task AddAsync(CallAnalysis analysis, CancellationToken cancellationToken = default)
    {
        await _db.CallAnalyses.AddAsync(analysis, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CallAnalysis?> GetByCallIdAsync(Guid callId, CancellationToken cancellationToken = default)
        => await _db.CallAnalyses.FirstOrDefaultAsync(a => a.CallId == callId, cancellationToken);
}
