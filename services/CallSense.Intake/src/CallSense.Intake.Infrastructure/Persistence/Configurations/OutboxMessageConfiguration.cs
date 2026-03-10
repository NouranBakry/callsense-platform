using CallSense.Infrastructure.Shared.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallSense.Intake.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Type).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Payload).IsRequired().HasColumnType("text");
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.HasIndex(o => new { o.ProcessedAt, o.CreatedAt });
    }
}
