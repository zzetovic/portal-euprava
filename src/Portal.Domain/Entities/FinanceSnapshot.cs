using Portal.Domain.Common;

namespace Portal.Domain.Entities;

public class FinanceSnapshot : TenantEntity
{
    public string Oib { get; set; } = default!;
    public DateTime FetchedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Payload { get; set; } // jsonb
}
