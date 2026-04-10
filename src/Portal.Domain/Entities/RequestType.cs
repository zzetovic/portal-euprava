using Portal.Domain.Common;

namespace Portal.Domain.Entities;

public class RequestType : TenantEntity
{
    public string Code { get; set; } = default!;
    public string? NameI18n { get; set; } // jsonb: {"hr":"...","en":"..."}
    public string? DescriptionI18n { get; set; } // jsonb
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
    public int SortOrder { get; set; }
    public int Version { get; set; } = 1;
    public int? EstimatedProcessingDays { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Tenant Tenant { get; set; } = default!;
    public ICollection<RequestTypeField> Fields { get; set; } = new List<RequestTypeField>();
    public ICollection<RequestTypeAttachment> Attachments { get; set; } = new List<RequestTypeAttachment>();
}
