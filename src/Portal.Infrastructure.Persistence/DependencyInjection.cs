using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portal.Application.Interfaces;
using Portal.Infrastructure.Persistence.Middleware;

namespace Portal.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<TenantConnectionInterceptor>();

        services.AddDbContext<PortalDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("PortalDb"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(PortalDbContext).Assembly.FullName);
                });

            options.UseSnakeCaseNamingConvention();

            var interceptor = sp.GetRequiredService<TenantConnectionInterceptor>();
            options.AddInterceptors(interceptor);
        });

        services.AddScoped<IPortalDbContext>(sp => sp.GetRequiredService<PortalDbContext>());

        return services;
    }
}
