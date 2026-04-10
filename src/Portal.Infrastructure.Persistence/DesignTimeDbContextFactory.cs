using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Portal.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PortalDbContext>
{
    public PortalDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PortalDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=portal_euprava;Username=portal;Password=portal_dev",
            npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(PortalDbContext).Assembly.FullName));
        optionsBuilder.UseSnakeCaseNamingConvention();

        return new PortalDbContext(optionsBuilder.Options);
    }
}
