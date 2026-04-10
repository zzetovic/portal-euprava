using Portal.Application.Interfaces;

namespace Portal.Api.Services;

public class HttpTenantProvider(IHttpContextAccessor httpContextAccessor) : ITenantProvider
{
    public Guid GetCurrentTenantId()
    {
        if (httpContextAccessor.HttpContext?.Items["TenantId"] is Guid tenantId)
            return tenantId;

        return Guid.Empty;
    }

    public Task<Guid> ResolveTenantIdAsync(string tenantCode, CancellationToken ct)
    {
        return Task.FromResult(GetCurrentTenantId());
    }
}
