using Portal.Domain.Common;
using Portal.Domain.Enums;

namespace Portal.Domain.Entities;

public class Request : TenantEntity
{
    public Guid CitizenId { get; set; }
    public Guid RequestTypeId { get; set; }
    public int RequestTypeVersion { get; set; }
    public string ReferenceNumber { get; set; } = default!;
    public RequestStatus Status { get; set; }
    public string FormData { get; set; } = default!; // jsonb
    public string FormSchemaSnapshot { get; set; } = default!; // jsonb
    public DateTime? SubmittedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReasonCode { get; set; }
    public string? RejectionInternalNote { get; set; }
    public DateTime? ViewedFirstAt { get; set; }
    public Guid? ViewedFirstByUserId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsLockedToOldVersion { get; set; }
    public string? Etag { get; set; }

    public Tenant Tenant { get; set; } = default!;
    public User Citizen { get; set; } = default!;
    public RequestType RequestType { get; set; } = default!;
    public User? ReviewedByUser { get; set; }
    public User? ViewedFirstByUser { get; set; }
    public ICollection<RequestAttachment> Attachments { get; set; } = new List<RequestAttachment>();
    public ICollection<RequestStatusHistory> StatusHistory { get; set; } = new List<RequestStatusHistory>();
    public SeupAktMapping? AktMapping { get; set; }
}
