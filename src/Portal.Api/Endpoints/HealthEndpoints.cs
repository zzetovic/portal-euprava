using Portal.Infrastructure.Persistence;

namespace Portal.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/health").WithTags("Health");

        group.MapGet("/live", () => Results.Ok(new { status = "healthy" }))
            .AllowAnonymous();

        group.MapGet("/ready", async (PortalDbContext db) =>
        {
            try
            {
                var canConnect = await db.Database.CanConnectAsync();
                return canConnect
                    ? Results.Ok(new { status = "ready" })
                    : Results.Json(new { status = "unavailable" }, statusCode: 503);
            }
            catch
            {
                return Results.Json(new { status = "unavailable" }, statusCode: 503);
            }
        }).AllowAnonymous();
    }
}
