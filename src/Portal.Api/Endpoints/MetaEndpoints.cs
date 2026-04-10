using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;

namespace Portal.Api.Endpoints;

public static class MetaEndpoints
{
    public static void MapMetaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/meta").WithTags("Meta");

        group.MapGet("/tenant", async (
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IPortalDbContext db) =>
        {
            var tenantId = tenantProvider.GetCurrentTenantId();
            if (tenantId == Guid.Empty)
                return Results.BadRequest(new { detail = "Tenant not resolved." });

            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
                return Results.NotFound();

            return Results.Ok(new
            {
                tenant.Id,
                tenant.Code,
                tenant.Name,
                tenant.Settings
            });
        }).RequireAuthorization();
    }
}
