using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace CleanHr.EmployeeApi.Application.Caching.Handlers;

[ScopedService]
public interface IDepartmentCacheHandler
{
    Task RemoveListAsync();
}
