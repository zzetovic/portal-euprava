using Portal.Application.Interfaces;

namespace Portal.Api.Cli;

public class NullTenantProvider : ITenantProvider
{
    public Guid GetCurrentTenantId() => Guid.Empty;

    public Task<Guid> ResolveTenantIdAsync(string tenantCode, CancellationToken ct)
        => Task.FromResult(Guid.Empty);
}
