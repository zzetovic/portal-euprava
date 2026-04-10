using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Infrastructure.Persistence.Configurations;

public class RequestTypeConfiguration : IEntityTypeConfiguration<RequestType>
{
    public void Configure(EntityTypeBuilder<RequestType> builder)
    {
        builder.ToTable("request_types");

        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(rt => rt.Code).HasMaxLength(64).IsRequired();
        builder.Property(rt => rt.NameI18n).HasColumnType("jsonb");
        builder.Property(rt => rt.DescriptionI18n).HasColumnType("jsonb");
        builder.Property(rt => rt.IsActive).HasDefaultValue(true);
        builder.Property(rt => rt.IsArchived).HasDefaultValue(false);
        builder.Property(rt => rt.Version).HasDefaultValue(1);

        builder.HasIndex(rt => new { rt.TenantId, rt.Code }).IsUnique();

        builder.HasOne(rt => rt.Tenant)
            .WithMany(t => t.RequestTypes)
            .HasForeignKey(rt => rt.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(rt => rt.DeletedAt == null);
    }
}

public class RequestTypeFieldConfiguration : IEntityTypeConfiguration<RequestTypeField>
{
    public void Configure(EntityTypeBuilder<RequestTypeField> builder)
    {
        builder.ToTable("request_type_fields");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(f => f.FieldKey).HasMaxLength(64).IsRequired();
        builder.Property(f => f.LabelI18n).HasColumnType("jsonb");
        builder.Property(f => f.HelpTextI18n).HasColumnType("jsonb");
        builder.Property(f => f.FieldType).HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(f => f.ValidationRules).HasColumnType("jsonb");
        builder.Property(f => f.Options).HasColumnType("jsonb");

        builder.HasIndex(f => new { f.RequestTypeId, f.FieldKey }).IsUnique();

        builder.HasOne(f => f.RequestType)
            .WithMany(rt => rt.Fields)
            .HasForeignKey(f => f.RequestTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RequestTypeAttachmentConfiguration : IEntityTypeConfiguration<RequestTypeAttachment>
{
    public void Configure(EntityTypeBuilder<RequestTypeAttachment> builder)
    {
        builder.ToTable("request_type_attachments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.AttachmentKey).HasMaxLength(64).IsRequired();
        builder.Property(a => a.LabelI18n).HasColumnType("jsonb");
        builder.Property(a => a.DescriptionI18n).HasColumnType("jsonb");

        builder.HasOne(a => a.RequestType)
            .WithMany(rt => rt.Attachments)
            .HasForeignKey(a => a.RequestTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
