using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanHr.EmployeeApi.Domain.Aggregates;

public interface IDepartmentServiceClient
{
    Task<bool> IsDepartmentExistentAsync(Guid departmentId, CancellationToken cancellationToken = default);
}
