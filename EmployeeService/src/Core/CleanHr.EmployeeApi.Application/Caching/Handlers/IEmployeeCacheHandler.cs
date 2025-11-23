namespace CleanHr.EmployeeApi.Application.Caching.Handlers;

public interface IEmployeeCacheHandler
{
    Task RemoveDetailsByIdAsync(Guid employeeId);
}
