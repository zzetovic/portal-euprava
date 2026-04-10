using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Infrastructure.Persistence.Configurations;

public class SeupAktMappingConfiguration : IEntityTypeConfiguration<SeupAktMapping>
{
    public void Configure(EntityTypeBuilder<SeupAktMapping> builder)
    {
        builder.ToTable("seup_akt_mappings");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasIndex(m => m.RequestId).IsUnique();
        builder.HasIndex(m => new { m.TenantId, m.RequestId }).IsUnique();

        builder.HasOne(m => m.Request)
            .WithOne(r => r.AktMapping)
            .HasForeignKey<SeupAktMapping>(m => m.RequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.ReceivedByUser)
            .WithMany()
            .HasForeignKey(m => m.ReceivedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
