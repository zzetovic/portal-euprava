using Portal.Domain.Common;

namespace Portal.Domain.Entities;

public class RequestTypeAttachment : BaseEntity
{
    public Guid RequestTypeId { get; set; }
    public string AttachmentKey { get; set; } = default!;
    public string? LabelI18n { get; set; } // jsonb
    public string? DescriptionI18n { get; set; } // jsonb
    public bool IsRequired { get; set; }
    public long MaxSizeBytes { get; set; }
    public string[] AllowedMimeTypes { get; set; } = Array.Empty<string>();
    public int SortOrder { get; set; }

    public RequestType RequestType { get; set; } = default!;
}
