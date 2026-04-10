using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Infrastructure.Persistence.Configurations;

public class FinanceSnapshotConfiguration : IEntityTypeConfiguration<FinanceSnapshot>
{
    public void Configure(EntityTypeBuilder<FinanceSnapshot> builder)
    {
        builder.ToTable("finance_snapshots");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(f => f.Oib).HasMaxLength(11).IsRequired();
        builder.Property(f => f.Payload).HasColumnType("jsonb");

        builder.HasIndex(f => new { f.TenantId, f.Oib, f.ExpiresAt });
    }
}
