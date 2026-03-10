using CallSense.Analysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallSense.Analysis.Infrastructure.Persistence;

public class AnalysisDbContext : DbContext
{
    public AnalysisDbContext(DbContextOptions<AnalysisDbContext> options) : base(options) { }
    public DbSet<CallAnalysis> CallAnalyses => Set<CallAnalysis>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnalysisDbContext).Assembly);
}
