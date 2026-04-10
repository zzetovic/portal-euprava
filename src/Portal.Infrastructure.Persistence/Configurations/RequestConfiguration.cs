using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Infrastructure.Persistence.Configurations;

public class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.ToTable("requests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.ReferenceNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(r => r.ReferenceNumber).IsUnique();

        builder.Property(r => r.Status).HasMaxLength(32).HasConversion<string>().IsRequired();
        builder.Property(r => r.FormData).HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.FormSchemaSnapshot).HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.RejectionReasonCode).HasMaxLength(50);
        builder.Property(r => r.IsLockedToOldVersion).HasDefaultValue(false);
        builder.Property(r => r.Etag).HasMaxLength(64);

        builder.HasIndex(r => new { r.TenantId, r.Status, r.SubmittedAt });

        builder.HasOne(r => r.Tenant)
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Citizen)
            .WithMany()
            .HasForeignKey(r => r.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RequestType)
            .WithMany()
            .HasForeignKey(r => r.RequestTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReviewedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.ViewedFirstByUser)
            .WithMany()
            .HasForeignKey(r => r.ViewedFirstByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class RequestAttachmentConfiguration : IEntityTypeConfiguration<RequestAttachment>
{
    public void Configure(EntityTypeBuilder<RequestAttachment> builder)
    {
        builder.ToTable("request_attachments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.AttachmentKey).HasMaxLength(64).IsRequired();
        builder.Property(a => a.OriginalFilename).HasMaxLength(255).IsRequired();
        builder.Property(a => a.MimeType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(a => a.ChecksumSha256).HasMaxLength(64).IsRequired();

        builder.HasOne(a => a.Request)
            .WithMany(r => r.Attachments)
            .HasForeignKey(a => a.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.UploadedByUser)
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RequestStatusHistoryConfiguration : IEntityTypeConfiguration<RequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<RequestStatusHistory> builder)
    {
        builder.ToTable("request_status_history");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(h => h.FromStatus).HasMaxLength(32).HasConversion<string>();
        builder.Property(h => h.ToStatus).HasMaxLength(32).HasConversion<string>().IsRequired();
        builder.Property(h => h.ChangedBySource).HasMaxLength(20).HasConversion<string>().IsRequired();

        builder.HasOne(h => h.Request)
            .WithMany(r => r.StatusHistory)
            .HasForeignKey(h => h.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
