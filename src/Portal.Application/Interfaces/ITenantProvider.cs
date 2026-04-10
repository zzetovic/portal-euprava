namespace Portal.Application.Interfaces;

public interface ITenantProvider
{
    Guid GetCurrentTenantId();
    Task<Guid> ResolveTenantIdAsync(string tenantCode, CancellationToken ct);
}
