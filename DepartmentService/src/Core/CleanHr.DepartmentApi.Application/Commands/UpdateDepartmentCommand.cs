using System.Diagnostics;
using CleanHr.DepartmentApi.Application.Caching.Handlers;
using CleanHr.DepartmentApi.Domain;
using CleanHr.DepartmentApi.Domain.Aggregates;
using MediatR;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.DepartmentApi.Application.Commands;

public sealed record UpdateDepartmentCommand(
    Guid Id,
    string Name,
    string Description,
    bool IsActive) : IRequest<Result>;

internal sealed class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, Result>
{
    private static readonly ActivitySource ActivitySource = new("CleanHr.DepartmentApi");
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDepartmentCacheHandler _departmentCacheHandler;
    private readonly ILogger<UpdateDepartmentCommandHandler> _logger;

    public UpdateDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        IDepartmentCacheHandler departmentCacheHandler,
        ILogger<UpdateDepartmentCommandHandler> logger)
    {
        _departmentRepository = departmentRepository ?? throw new ArgumentNullException(nameof(departmentRepository));
        _departmentCacheHandler = departmentCacheHandler ?? throw new ArgumentNullException(nameof(departmentCacheHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("UpdateDepartment", ActivityKind.Internal);
        activity?.SetTag("department.id", request.Id.ToString());
        activity?.SetTag("department.name", request.Name);

        using var loggerScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["DepartmentUpdateRequest"] = request
        });

        try
        {
            request.ThrowIfNull(nameof(request));

            _logger.LogInformation("Updating department with DepartmentId: {DepartmentId}", request.Id);

            Department departmentToBeUpdated = await _departmentRepository.GetByIdAsync(request.Id);

            if (departmentToBeUpdated == null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Department not found");
                _logger.LogWarning("Department not found. DepartmentId: {DepartmentId}", request.Id);
                return Result.Failure("DepartmentId", $"The department with id '{request.Id}' was not found.");
            }

            Result setNameResult = await departmentToBeUpdated.SetNameAsync(_departmentRepository, request.Name);
            if (setNameResult.IsSuccess == false)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Failed to set department name");
                _logger.LogWarning("Failed to set department name. DepartmentId: {DepartmentId}, Error: {Error}", request.Id, setNameResult.Error);
                return setNameResult;
            }

            Result setDescriptionResult = departmentToBeUpdated.SetDescription(request.Description);
            if (setDescriptionResult.IsSuccess == false)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Failed to set department description");
                _logger.LogWarning("Failed to set department description. DepartmentId: {DepartmentId}, Error: {Error}", request.Id, setDescriptionResult.Error);
                return setDescriptionResult;
            }

            await _departmentRepository.UpdateAsync(departmentToBeUpdated);

            await _departmentCacheHandler.RemoveListAsync();

            activity?.SetStatus(ActivityStatusCode.Ok, "Department updated successfully");
            _logger.LogInformation("Department updated successfully. DepartmentId: {DepartmentId}, Name: {Name}", request.Id, request.Name);

            return Result.Success();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Exception occurred while updating department. DepartmentId: {DepartmentId}", request?.Id);
            return Result.Failure("Exception", "An error occurred while updating the department.");
        }
    }
}
