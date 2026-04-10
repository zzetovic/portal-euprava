using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_log");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Before).HasColumnType("jsonb");
        builder.Property(a => a.After).HasColumnType("jsonb");
        builder.Property(a => a.Ip).HasMaxLength(45);

        builder.HasIndex(a => new { a.TenantId, a.CreatedAt })
            .IsDescending(false, true);
        builder.HasIndex(a => new { a.TenantId, a.EntityType, a.EntityId });
    }
}
