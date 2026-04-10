using Portal.Application.Interfaces;

namespace Portal.Infrastructure.LocalDb;

public class LocalDbFinanceReader : IFinanceReader
{
    public Task<FinanceSnapshot> GetByOibAsync(Guid tenantId, string oib, CancellationToken ct)
    {
        // TODO: Implement when finance module schema is available
        throw new NotImplementedException("LocalDbFinanceReader is not yet implemented.");
    }
}
