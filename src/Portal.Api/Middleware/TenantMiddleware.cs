namespace Portal.Api.Middleware;

public class TenantMiddleware(RequestDelegate next)
{
    private const string TenantHeader = "X-Tenant-Code";

    public async Task InvokeAsync(HttpContext context)
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
                context.Items["TenantCode"] = tenantCode.ToString();
            }
        }

        await next(context);
    }

    private static bool IsPublicEndpoint(PathString path)
    {
        return path.StartsWithSegments("/api/v1/health")
            || path.StartsWithSegments("/api/v1/meta");
    }
}
