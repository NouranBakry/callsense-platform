using CallSense.Ingestion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallSense.Ingestion.Infrastructure.Persistence.Configurations;

// IEntityTypeConfiguration<CallRecord> is the Fluent API entry point for this entity.
// EF calls Configure() during OnModelCreating via ApplyConfigurationsFromAssembly.
// internal sealed = not part of any public API, can't be subclassed accidentally.
internal sealed class CallRecordConfiguration : IEntityTypeConfiguration<CallRecord>
{
    public void Configure(EntityTypeBuilder<CallRecord> builder)
    {
        // Every entity needs a primary key. EF can infer it from a property
        // named "Id", but being explicit is clearer for readers.
        builder.HasKey(c => c.Id);

        // TenantId is mandatory — every call must belong to a tenant.
        // IsRequired() maps to NOT NULL in PostgreSQL.
        builder.Property(c => c.TenantId)
            .IsRequired();

        // VARCHAR(255) — long enough for any real file name, short enough for indexing.
        builder.Property(c => c.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        // Blob URLs can include query strings (SAS tokens) — 2048 is a safe max.
        builder.Property(c => c.BlobUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(c => c.FileSizeBytes)
            .IsRequired();

        // HasConversion<string>() stores the enum value as its name ("Uploaded",
        // "Transcribing", etc.) instead of its integer ordinal (0, 1, 2...).
        // String storage makes the database human-readable and is safe to reorder.
        // If you ever change the enum order, integer storage would silently corrupt data.
        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Index on TenantId because the most common query is
        // "give me all calls for this tenant" — without an index
        // that's a full table scan on a table that grows indefinitely.
        builder.HasIndex(c => c.TenantId);
    }
}
