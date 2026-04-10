using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Infrastructure.Persistence.Configurations;

public class IntegrationOutboxConfiguration : IEntityTypeConfiguration<IntegrationOutbox>
{
    public void Configure(EntityTypeBuilder<IntegrationOutbox> builder)
    {
        builder.ToTable("integration_outbox");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(o => o.AggregateType).HasMaxLength(50).IsRequired();
        builder.Property(o => o.Operation).HasMaxLength(50).IsRequired();
        builder.Property(o => o.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.HasIndex(o => o.IdempotencyKey).IsUnique();

        builder.Property(o => o.Payload).HasColumnType("jsonb");
        builder.Property(o => o.Status).HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(o => o.Attempts).HasDefaultValue(0);

        builder.HasIndex(o => new { o.Status, o.NextAttemptAt });
    }
}
