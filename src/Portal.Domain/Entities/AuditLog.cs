using Portal.Domain.Common;

namespace Portal.Domain.Entities;

public class AuditLog : TenantEntity
{
    public Guid? UserId { get; set; }
    public string Action { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public Guid? EntityId { get; set; }
    public string? Before { get; set; } // jsonb
    public string? After { get; set; } // jsonb
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
}
