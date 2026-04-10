using Portal.Domain.Common;
using Portal.Domain.Enums;

namespace Portal.Domain.Entities;

public class RequestTypeField : BaseEntity
{
    public Guid RequestTypeId { get; set; }
    public string FieldKey { get; set; } = default!;
    public string? LabelI18n { get; set; } // jsonb
    public string? HelpTextI18n { get; set; } // jsonb
    public FieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public string? ValidationRules { get; set; } // jsonb
    public string? Options { get; set; } // jsonb
    public int SortOrder { get; set; }

    public RequestType RequestType { get; set; } = default!;
}
