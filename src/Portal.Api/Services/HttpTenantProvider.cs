using Portal.Application.Interfaces;

namespace Portal.Api.Services;

public class HttpTenantProvider(IHttpContextAccessor httpContextAccessor, IPortalDbContext dbContext) : ITenantProvider
{
    private Guid? _resolvedTenantId;

    public Guid GetCurrentTenantId()
    {
        if (_resolvedTenantId.HasValue)
            return _resolvedTenantId.Value;

        var tenantCode = httpContextAccessor.HttpContext?.Items["TenantCode"]?.ToString();
        if (string.IsNullOrEmpty(tenantCode))
            return Guid.Empty;

        var tenant = dbContext.Tenants.FirstOrDefault(t => t.Code == tenantCode && t.IsActive);
        _resolvedTenantId = tenant?.Id ?? Guid.Empty;
        return _resolvedTenantId.Value;
    }

    public async Task<Guid> ResolveTenantIdAsync(string tenantCode, CancellationToken ct)
    {
        var tenant = dbContext.Tenants.FirstOrDefault(t => t.Code == tenantCode && t.IsActive);
        _resolvedTenantId = tenant?.Id ?? Guid.Empty;
        return _resolvedTenantId.Value;
    }
}
