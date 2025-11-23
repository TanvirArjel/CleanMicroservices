using CleanHr.DepartmentApi.Application.Queries.DepartmentQueries;
using CleanHr.DepartmentApi.Domain.Aggregates;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace CleanHr.DepartmentApi.Application.Caching.Repositories;

[ScopedService]
public interface IDepartmentCacheRepository
{
    Task<List<DepartmentDto>> GetListAsync();

    Task<Department> GetByIdAsync(Guid departmentId);

    Task<DepartmentDetailsDto> GetDetailsByIdAsync(Guid departmentId);
}
