using CallSense.Analysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallSense.Analysis.Infrastructure.Persistence.Configurations;

internal sealed class CallAnalysisConfiguration : IEntityTypeConfiguration<CallAnalysis>
{
    public void Configure(EntityTypeBuilder<CallAnalysis> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.CallId).IsRequired();
        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.Report).HasColumnType("text");
        builder.Property(a => a.Strengths).HasColumnType("text");
        builder.Property(a => a.Improvements).HasColumnType("text");
        builder.Property(a => a.ErrorMessage).HasColumnType("text");
        builder.HasIndex(a => a.CallId).IsUnique();
        builder.HasIndex(a => a.TenantId);
    }
}
