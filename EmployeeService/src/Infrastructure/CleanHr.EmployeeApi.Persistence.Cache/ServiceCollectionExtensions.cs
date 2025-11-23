using CleanHr.EmployeeApi.Application.Caching.Handlers;
using CleanHr.EmployeeApi.Persistence.Cache.Handlers;
using Microsoft.Extensions.DependencyInjection;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.EmployeeApi.Persistence.Cache;

public static class ServiceCollectionExtensions
{
    public static void AddCaching(this IServiceCollection services)
    {
        services.ThrowIfNull(nameof(services));

        services.AddDistributedMemoryCache();

        services.AddScoped<IEmployeeCacheHandler, EmployeeCacheHandler>();
    }
}
