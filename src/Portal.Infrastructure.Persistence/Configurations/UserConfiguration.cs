using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.Email).HasColumnType("citext").IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(255);
        builder.Property(u => u.Oib).HasMaxLength(11);
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Phone).HasMaxLength(32);
        builder.Property(u => u.UserType).HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.MustChangePassword).HasDefaultValue(false);
        builder.Property(u => u.PreferredLanguage).HasMaxLength(5).HasDefaultValue("hr");

        builder.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();

        builder.HasOne(u => u.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}
