using CallSense.Infrastructure.Shared.Outbox;
using CallSense.Intake.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallSense.Intake.Infrastructure.Persistence;

public class IntakeDbContext : DbContext
{
    public IntakeDbContext(DbContextOptions<IntakeDbContext> options) : base(options) { }

    public DbSet<CallRecord> CallRecords => Set<CallRecord>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IntakeDbContext).Assembly);
    }
}
