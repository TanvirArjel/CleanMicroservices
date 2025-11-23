using CleanHr.EmployeeApi.Application.Queries;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace CleanHr.EmployeeApi.Application.Caching.Repositories;

[ScopedService]
public interface IEmployeeCacheRepository
{
    Task<EmployeeDetailsDto> GetDetailsByIdAsync(Guid employeeId);
}
