using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CleanHr.DepartmentApi.Domain.Aggregates;

public interface IDepartmentRepository
{
    Task<Department> GetByIdAsync(Guid departmentId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Expression<Func<Department, bool>> condition, CancellationToken cancellationToken);

    Task InsertAsync(Department department, CancellationToken cancellationToken);

    Task UpdateAsync(Department department, CancellationToken cancellationToken);

    Task DeleteAsync(Department department, CancellationToken cancellationToken);
}
