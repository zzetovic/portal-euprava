using Portal.Domain.Common;
using Portal.Domain.Enums;

namespace Portal.Domain.Entities;

public class IntegrationOutbox : TenantEntity
{
    public string AggregateType { get; set; } = default!;
    public Guid AggregateId { get; set; }
    public string Operation { get; set; } = default!;
    public string IdempotencyKey { get; set; } = default!;
    public string? Payload { get; set; } // jsonb
    public OutboxStatus Status { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
