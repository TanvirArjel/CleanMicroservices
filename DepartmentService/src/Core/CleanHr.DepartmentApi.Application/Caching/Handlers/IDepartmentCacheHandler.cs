using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace CleanHr.DepartmentApi.Application.Caching.Handlers;

[ScopedService]
public interface IDepartmentCacheHandler
{
    Task RemoveListAsync();
}
