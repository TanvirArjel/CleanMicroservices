using System;
using System.Threading.Tasks;
using CleanHr.EmployeeApi.Application.Caching.Handlers;
using CleanHr.EmployeeApi.Persistence.Cache.Keys;
using Microsoft.Extensions.Caching.Distributed;

namespace CleanHr.EmployeeApi.Persistence.Cache.Handlers;

internal sealed class EmployeeCacheHandler(IDistributedCache distributedCache) : IEmployeeCacheHandler
{
    private readonly IDistributedCache _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));

    public async Task RemoveDetailsByIdAsync(Guid employeeId)
    {
        string detailsKey = EmployeeCacheKeys.GetDetailsKey(employeeId);
        await _distributedCache.RemoveAsync(detailsKey);
    }
}
