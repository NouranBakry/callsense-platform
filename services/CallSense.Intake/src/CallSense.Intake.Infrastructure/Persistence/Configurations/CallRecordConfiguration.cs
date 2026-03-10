using CallSense.Intake.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallSense.Intake.Infrastructure.Persistence.Configurations;

internal sealed class CallRecordConfiguration : IEntityTypeConfiguration<CallRecord>
{
    public void Configure(EntityTypeBuilder<CallRecord> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.OriginalFileName).IsRequired().HasMaxLength(255);
        builder.Property(c => c.BlobUrl).IsRequired().HasMaxLength(2048);
        builder.Property(c => c.FileSizeBytes).IsRequired();
        builder.Property(c => c.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.TranscriptText).HasColumnType("text");
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.HasIndex(c => c.TenantId);
    }
}
