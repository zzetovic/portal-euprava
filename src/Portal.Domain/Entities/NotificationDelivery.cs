using Portal.Domain.Common;
using Portal.Domain.Enums;

namespace Portal.Domain.Entities;

public class NotificationDelivery : BaseEntity
{
    public Guid NotificationId { get; set; }
    public NotificationChannel Channel { get; set; }
    public DeliveryStatus Status { get; set; }
    public string? ProviderMessageId { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime? SentAt { get; set; }

    public Notification Notification { get; set; } = default!;
}
