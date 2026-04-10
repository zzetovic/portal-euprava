using Portal.Domain.Common;

namespace Portal.Domain.Entities;

public class Notification : TenantEntity
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = default!;
    public string? TitleI18n { get; set; } // jsonb
    public string? BodyI18n { get; set; } // jsonb
    public Guid? RelatedRequestId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public User User { get; set; } = default!;
    public Request? RelatedRequest { get; set; }
    public ICollection<NotificationDelivery> Deliveries { get; set; } = new List<NotificationDelivery>();
}
