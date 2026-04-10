using System.Security.Claims;
using Portal.Application.Interfaces;

namespace Portal.Api.Middleware;

public class TenantJwtCrossCheckMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.Items.TryGetValue("TenantCode", out var tenantCodeObj)
            && tenantCodeObj is string tenantCode
            && !string.IsNullOrEmpty(tenantCode))
        {
            var jwtTenantClaim = context.User.FindFirstValue("tenant_id");
            if (jwtTenantClaim is not null && Guid.TryParse(jwtTenantClaim, out var jwtTenantId))
            {
                var tenantProvider = context.RequestServices.GetRequiredService<ITenantProvider>();
                var resolvedTenantId = await tenantProvider.ResolveTenantIdAsync(tenantCode, CancellationToken.None);

                if (resolvedTenantId != Guid.Empty && resolvedTenantId != jwtTenantId)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        type = "https://tools.ietf.org/html/rfc7807",
                        title = "Tenant mismatch",
                        status = 403,
                        detail = "JWT tenant claim does not match the X-Tenant-Code header."
                    });
                    return;
                }
            }
        }

        await next(context);
    }
}
