using System.Diagnostics;
using CleanHr.DepartmentApi.Application.Caching.Handlers;
using CleanHr.DepartmentApi.Domain;
using CleanHr.DepartmentApi.Domain.Aggregates;
using MediatR;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.DepartmentApi.Application.Commands;

public sealed record CreateDepartmentCommand(string Name, string Description) : IRequest<Result<Guid>>;

internal class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Result<Guid>>
{
    private static readonly ActivitySource ActivitySource = new("CleanHr.DepartmentApi");
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDepartmentCacheHandler _departmentCacheHandler;
    private readonly ILogger<CreateDepartmentCommandHandler> _logger;

    public CreateDepartmentCommandHandler(
            IDepartmentRepository departmentRepository,
            IDepartmentCacheHandler departmentCacheHandler,
            ILogger<CreateDepartmentCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(departmentRepository);
        ArgumentNullException.ThrowIfNull(departmentCacheHandler);
        ArgumentNullException.ThrowIfNull(logger);

        _departmentRepository = departmentRepository;
        _departmentCacheHandler = departmentCacheHandler;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("CreateDepartment", ActivityKind.Internal);
        activity?.SetTag("department.name", request.Name);

        using var loggerScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["DepartmentCreationRequest"] = request
        });

        try
        {
            _ = request.ThrowIfNull(nameof(request));

            _logger.LogInformation("Creating department.");

            Result<Department> result = await Department.CreateAsync(
                _departmentRepository,
                request.Name,
                request.Description);

            if (result.IsSuccess == false)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Department creation failed");
                _logger.LogWarning("Department creation failed with Errors: {Errors}", result.Error);
                return Result<Guid>.Failure(result.Error);
            }

            // Persist to the database
            await _departmentRepository.InsertAsync(result.Value, cancellationToken);

            // Remove the cache
            await _departmentCacheHandler.RemoveListAsync();

            activity?.SetStatus(ActivityStatusCode.Ok, "Department created successfully");
            _logger.LogInformation("Department created successfully. DepartmentId: {DepartmentId}", result.Value.Id);

            return Result<Guid>.Success(result.Value.Id);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Exception occurred while creating department");
            return Result<Guid>.Failure("Exception", "An error occurred while creating the department.");
        }
    }
}