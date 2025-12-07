using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CleanHr.DepartmentApi.Domain;
using CleanHr.DepartmentApi.Domain.Aggregates;
using CleanHr.DepartmentApi.Persistence.RelationalDB.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.DepartmentApi.Persistence.RelationalDB.Repositories;

internal sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly CleanHrDbContext _dbContext;
    private readonly ILogger<DepartmentRepository> _logger;

    public DepartmentRepository(CleanHrDbContext dbContext, ILogger<DepartmentRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<bool>> ExistsAsync(
        Expression<Func<Department, bool>> condition,
        CancellationToken cancellationToken = default)
    {
        using Activity activity = InfrastructureActivityConstants.Source.StartActivity(
            "ExistsAsync",
            ActivityKind.Internal);

        try
        {
            IQueryable<Department> queryable = _dbContext.Set<Department>();

            if (condition != null)
            {
                queryable = queryable.Where(condition);
            }

            bool exists = await queryable.AnyAsync(cancellationToken);
            activity.SetStatus(ActivityStatusCode.Ok, "Checked existence of department successfully");
            _logger.LogInformation("Checked existence of department, exists: {Exists}", exists);
            return Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error occurred while checking existence of department");
            return Result<bool>.Failure("Exception", "Error occurred while checking existence of department.");
        }
    }

    public async Task<Result<Department>> GetByIdAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        using Activity activity = InfrastructureActivityConstants.Source.StartActivity(
            "GetByIdAsync",
            ActivityKind.Internal);
        activity?.SetTag("department.id", departmentId);

        try
        {
            departmentId.ThrowIfEmpty(nameof(departmentId));

            Department department = await _dbContext.Set<Department>().FindAsync([departmentId], cancellationToken);

            activity.SetStatus(ActivityStatusCode.Ok, "Fetched department by id successfully");
            _logger.LogInformation("Fetched department by id: {DepartmentId}, isFound : {IsFound}", departmentId, department != null);

            return Result<Department>.Success(department);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error occurred while fetching department by id: {DepartmentId}", departmentId);
            return Result<Department>.Failure("Exception", $"Error occurred while fetching department by id: {departmentId}.");
        }
    }

    public async Task<Result<Department>> InsertAsync(Department department, CancellationToken cancellationToken = default)
    {
        using Activity activity = InfrastructureActivityConstants.Source.StartActivity(
            "InsertAsync",
            ActivityKind.Internal);
        activity?.SetTag("department.id", department.Id);

        try
        {
            department.ThrowIfNull(nameof(department));

            await _dbContext.Set<Department>().AddAsync(department, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            activity.SetStatus(ActivityStatusCode.Ok, "Inserted new department successfully");
            _logger.LogInformation("Inserted new department with id: {DepartmentId}", department.Id);
            return Result<Department>.Success(department);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error occurred while inserting department with id: {DepartmentId}", department?.Id);
            return Result<Department>.Failure("Exception", $"Exception occurred while inserting department with id: {department?.Id}.");
        }
    }

    public async Task<Result<Department>> UpdateAsync(Department department, CancellationToken cancellationToken = default)
    {
        using Activity activity = InfrastructureActivityConstants.Source.StartActivity(
            "UpdateAsync",
            ActivityKind.Internal);
        activity?.SetTag("department.id", department.Id);

        try
        {
            department.ThrowIfNull(nameof(department));

            EntityEntry<Department> trackedEntity = _dbContext.ChangeTracker.Entries<Department>()
                .FirstOrDefault(x => x.Entity == department);
            if (trackedEntity == null)
            {
                _dbContext.Update(department);
            }
            await _dbContext.SaveChangesAsync(cancellationToken);

            activity.SetStatus(ActivityStatusCode.Ok, "Updated department successfully");
            _logger.LogInformation("Updated department with id: {DepartmentId}", department.Id);
            return Result<Department>.Success(department);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error occurred while updating department with id: {DepartmentId}", department?.Id);
            return Result<Department>.Failure("Exception", $"Error occurred while updating department with id: {department?.Id}.");
        }
    }

    public async Task<Result> DeleteAsync(Department department, CancellationToken cancellationToken = default)
    {
        using Activity activity = InfrastructureActivityConstants.Source.StartActivity(
            "DeleteAsync",
            ActivityKind.Internal);
        activity?.SetTag("department.id", department.Id);

        try
        {
            department.ThrowIfNull(nameof(department));

            _dbContext.Remove(department);
            await _dbContext.SaveChangesAsync(cancellationToken);

            activity.SetStatus(ActivityStatusCode.Ok, "Deleted department successfully");
            _logger.LogInformation("Deleted department with id: {DepartmentId}", department.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error occurred while deleting department with id: {DepartmentId}", department?.Id);
            return Result.Failure("Exception", $"Error occurred while deleting department with id: {department?.Id}.");
        }
    }
}
