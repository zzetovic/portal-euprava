using Microsoft.Extensions.DependencyInjection;
using Portal.Application.Interfaces;

namespace Portal.Infrastructure.LocalDb;

public static class DependencyInjection
{
    public static IServiceCollection AddLocalDb(this IServiceCollection services)
    {
        services.AddScoped<ILocalDbAktWriter, LocalDbAktWriter>();
        services.AddScoped<IFinanceReader, LocalDbFinanceReader>();

        return services;
    }
}
