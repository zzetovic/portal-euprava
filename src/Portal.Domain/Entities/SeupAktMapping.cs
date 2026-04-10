using Portal.Domain.Common;

namespace Portal.Domain.Entities;

public class SeupAktMapping : TenantEntity
{
    public Guid RequestId { get; set; }
    public long AktId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public Guid ReceivedByUserId { get; set; }

    public Request Request { get; set; } = default!;
    public User ReceivedByUser { get; set; } = default!;
}
