namespace Portal.Application.Interfaces;

public interface IFinanceReader
{
    Task<FinanceSnapshot> GetByOibAsync(
        Guid tenantId, string oib, CancellationToken ct);
}

public record FinanceSnapshot(
    string Oib,
    DateTime FetchedAt,
    DateTime ExpiresAt,
    string PayloadJson);
