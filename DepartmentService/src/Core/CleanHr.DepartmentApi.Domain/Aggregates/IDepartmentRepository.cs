using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CleanHr.DepartmentApi.Domain.Aggregates;

public interface IDepartmentRepository
{
    Task<Result<Department>> GetByIdAsync(Guid departmentId, CancellationToken cancellationToken);

    Task<Result<bool>> ExistsAsync(Expression<Func<Department, bool>> condition, CancellationToken cancellationToken);

    Task<Result<Department>> InsertAsync(Department department, CancellationToken cancellationToken);

    Task<Result<Department>> UpdateAsync(Department department, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Department department, CancellationToken cancellationToken);
}
