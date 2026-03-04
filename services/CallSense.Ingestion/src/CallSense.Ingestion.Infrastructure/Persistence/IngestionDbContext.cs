using CallSense.Infrastructure.Shared.Outbox;
using CallSense.Ingestion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallSense.Ingestion.Infrastructure.Persistence;

// DbContext is EF Core's unit-of-work + gateway to the database.
// One context per service — each microservice owns its own schema.
// IngestionDbContext only knows about tables this service writes.
public class IngestionDbContext : DbContext
{
    // Constructor accepts DbContextOptions — this is how DI passes the
    // connection string and provider (Npgsql) configured in DependencyInjection.cs.
    // You never call this constructor directly; the DI container does.
    public IngestionDbContext(DbContextOptions<IngestionDbContext> options)
        : base(options)
    {
    }

    // DbSet<T> = a table proxy. Calling db.CallRecords gives you a queryable
    // interface to the call_records table. EF tracks changes to objects you
    // retrieve through it, and SaveChangesAsync() flushes those changes.
    //
    // "=> Set<CallRecord>()" is the modern preferred form — it's lazily
    // evaluated and avoids the nullable warning that appears with
    // `public DbSet<CallRecord> CallRecords { get; set; }` without initialization.
    public DbSet<CallRecord> CallRecords => Set<CallRecord>();

    // The outbox table lives in the same database as CallRecords.
    // This is the key insight of the Transactional Outbox Pattern:
    // both rows are committed in one SQL transaction — atomically.
    // The OutboxPublisherWorker reads this table and publishes to RabbitMQ.
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ApplyConfigurationsFromAssembly scans this assembly for all classes
        // that implement IEntityTypeConfiguration<T> and applies them automatically.
        // CallRecordConfiguration and OutboxMessageConfiguration are picked up here.
        //
        // Why Fluent API over data annotations?
        // Data annotations put EF knowledge on the Domain entity — a layer violation.
        // Fluent configs live in Infrastructure, which is the correct layer.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IngestionDbContext).Assembly);
    }
}
