using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Infrastructure.Persistence.Configurations;

public class DeploymentBackupConfiguration : IEntityTypeConfiguration<DeploymentBackup>
{
    public void Configure(EntityTypeBuilder<DeploymentBackup> builder)
    {
        builder.ToTable("deployment_backups");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(b => b.Location).HasMaxLength(500).IsRequired();
    }
}
