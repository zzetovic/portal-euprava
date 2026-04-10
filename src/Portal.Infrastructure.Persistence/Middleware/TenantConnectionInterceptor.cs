using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Portal.Application.Interfaces;

namespace Portal.Infrastructure.Persistence.Middleware;

public class TenantConnectionInterceptor(ITenantProvider tenantProvider) : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        if (tenantId != Guid.Empty)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SET app.tenant_id = '{tenantId}'";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
