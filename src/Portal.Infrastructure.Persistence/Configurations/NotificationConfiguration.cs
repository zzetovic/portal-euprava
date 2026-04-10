using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(n => n.Type).HasMaxLength(50).IsRequired();
        builder.Property(n => n.TitleI18n).HasColumnType("jsonb");
        builder.Property(n => n.BodyI18n).HasColumnType("jsonb");
        builder.Property(n => n.IsRead).HasDefaultValue(false);

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.RelatedRequest)
            .WithMany()
            .HasForeignKey(n => n.RelatedRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_deliveries");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.Channel).HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(d => d.Status).HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(d => d.ProviderMessageId).HasMaxLength(255);
        builder.Property(d => d.Attempts).HasDefaultValue(0);

        builder.HasOne(d => d.Notification)
            .WithMany(n => n.Deliveries)
            .HasForeignKey(d => d.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
