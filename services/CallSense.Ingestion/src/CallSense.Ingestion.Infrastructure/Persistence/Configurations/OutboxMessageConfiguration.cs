using CallSense.Infrastructure.Shared.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallSense.Ingestion.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(m => m.Id);

        // Type is the message type name, e.g. "CallRecordingUploaded".
        // 200 chars is enough for any fully-qualified type name we'll ever use.
        builder.Property(m => m.Type)
            .IsRequired()
            .HasMaxLength(200);

        // Payload is a JSON string — no length limit (TEXT in PostgreSQL).
        // JSON payloads can be large (especially for future message types).
        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        // ProcessedAt is nullable — NULL means "not yet published to RabbitMQ".
        // The OutboxPublisherWorker queries WHERE ProcessedAt IS NULL.
        // No configuration needed for nullable (EF handles it automatically).

        // Composite index on (ProcessedAt, CreatedAt).
        // The worker's query: WHERE ProcessedAt IS NULL ORDER BY CreatedAt ASC LIMIT 20
        // This index makes that query a fast index scan instead of a full table scan.
        // Without it, as the outbox table grows the worker slows down — a classic
        // "works in dev, breaks in prod" trap.
        builder.HasIndex(m => new { m.ProcessedAt, m.CreatedAt });
    }
}
