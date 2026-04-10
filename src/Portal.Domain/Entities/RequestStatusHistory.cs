using Portal.Domain.Common;
using Portal.Domain.Enums;

namespace Portal.Domain.Entities;

public class RequestStatusHistory : BaseEntity
{
    public Guid RequestId { get; set; }
    public RequestStatus? FromStatus { get; set; }
    public RequestStatus ToStatus { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public StatusChangeSource ChangedBySource { get; set; }
    public string? Comment { get; set; }
    public DateTime ChangedAt { get; set; }

    public Request Request { get; set; } = default!;
    public User? ChangedByUser { get; set; }
}
