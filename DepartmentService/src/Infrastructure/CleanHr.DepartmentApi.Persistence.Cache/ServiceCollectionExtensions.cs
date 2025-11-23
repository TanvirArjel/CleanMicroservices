using Microsoft.Extensions.DependencyInjection;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.DepartmentApi.Persistence.Cache;

public static class ServiceCollectionExtensions
{
    public static void AddCaching(this IServiceCollection services)
    {
        services.ThrowIfNull(nameof(services));

        services.AddDistributedMemoryCache();
    }
}
