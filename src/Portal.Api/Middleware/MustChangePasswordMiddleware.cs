using Portal.Application.Interfaces;
using Portal.Domain.Enums;

namespace Portal.Api.Middleware;

public class MustChangePasswordMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowedPaths =
    [
        "/api/v1/auth/me",
        "/api/v1/auth/logout",
        "/api/v1/auth/password/reset",
        "/api/v1/auth/password/change",
        "/api/v1/auth/refresh"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.Request.Path.StartsWithSegments("/api"))
        {
            var currentUser = context.RequestServices.GetRequiredService<ICurrentUserService>();
            if (currentUser.UserId.HasValue && !IsAllowedPath(context.Request.Path))
            {
                var db = context.RequestServices.GetRequiredService<IPortalDbContext>();
                var user = await db.Users.FindAsync(currentUser.UserId.Value);

                if (user is not null && user.MustChangePassword)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        type = "https://tools.ietf.org/html/rfc7807",
                        title = "Password change required",
                        status = 403,
                        detail = "Morate promijeniti lozinku prije nastavka.",
                        code = "MUST_CHANGE_PASSWORD"
                    });
                    return;
                }
            }
        }

        await next(context);
    }

    private static bool IsAllowedPath(PathString path)
    {
        foreach (var allowed in AllowedPaths)
        {
            if (path.StartsWithSegments(allowed))
                return true;
        }
        return false;
    }
}
