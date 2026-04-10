using Microsoft.EntityFrameworkCore;
using Portal.Infrastructure.Persistence;

namespace Portal.Api.Middleware;

public class TenantMiddleware(RequestDelegate next)
{
    private const string TenantHeader = "X-Tenant-Code";

    public async Task InvokeAsync(HttpContext context, PortalDbContext dbContext)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            if (!context.Request.Headers.TryGetValue(TenantHeader, out var tenantCode)
                || string.IsNullOrWhiteSpace(tenantCode))
            {
                if (!IsPublicEndpoint(context.Request.Path))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        type = "https://tools.ietf.org/html/rfc7807",
                        title = "Missing tenant header",
                        status = 400,
                        detail = $"The {TenantHeader} header is required."
                    });
                    return;
                }
            }
            else
            {
                var code = tenantCode.ToString();
                context.Items["TenantCode"] = code;

                // Resolve tenant ID eagerly using a raw query to avoid the
                // TenantConnectionInterceptor recursion (the interceptor calls
                // GetCurrentTenantId which would query via the same DbContext).
                var conn = dbContext.Database.GetDbConnection();
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id FROM tenants WHERE code = @code AND is_active = true LIMIT 1";
                var param = cmd.CreateParameter();
                param.ParameterName = "code";
                param.Value = code;
                cmd.Parameters.Add(param);
                var result = await cmd.ExecuteScalarAsync();
                if (result is Guid tenantId)
                {
                    context.Items["TenantId"] = tenantId;
                }
            }
        }

        await next(context);
    }

    private static bool IsPublicEndpoint(PathString path)
    {
        return path.StartsWithSegments("/api/v1/health")
            || path.StartsWithSegments("/api/v1/meta")
            || path.StartsWithSegments("/api/v1/auth");
    }
}
