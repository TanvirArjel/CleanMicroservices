using System.Diagnostics;
using System.Linq.Expressions;
using CleanHr.DepartmentApi.Application.Constants;
using CleanHr.DepartmentApi.Domain;
using CleanHr.DepartmentApi.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.DepartmentApi.Application.Queries;

public sealed class GetDepartmentByIdQuery : IRequest<Result<DepartmentDetailsDto>>
{
    public Guid Id { get; }

    public GetDepartmentByIdQuery(Guid id)
    {
        Id = id;
    }

    // Handler
    private class GetDepartmentByIdQueryHandler : IRequestHandler<GetDepartmentByIdQuery, Result<DepartmentDetailsDto>>
    {
        private readonly IQueryRepository _repository;
        private readonly ILogger<GetDepartmentByIdQueryHandler> _logger;

        public GetDepartmentByIdQueryHandler(
            IQueryRepository repository,
            ILogger<GetDepartmentByIdQueryHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<DepartmentDetailsDto>> Handle(
            GetDepartmentByIdQuery request,
            CancellationToken cancellationToken)
        {
            using var activity = ApplicationActivityConstants.Source.StartActivity(
                "GetDepartmentById",
                ActivityKind.Internal);
            activity?.SetTag("department.id", request.Id.ToString());

            using var loggerScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["DepartmentId"] = request?.Id
            });

            try
            {
                request.ThrowIfNull(nameof(request));

                _logger.LogInformation("Retrieving department details. DepartmentId: {DepartmentId}", request.Id);

                Expression<Func<Department, DepartmentDetailsDto>> selectExp = d => new DepartmentDetailsDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    IsActive = d.IsActive,
                    CreatedAtUtc = d.CreatedAtUtc,
                    LastModifiedAtUtc = d.LastModifiedAtUtc
                };

                DepartmentDetailsDto departmentDetailsDto = await _repository.GetByIdAsync(request.Id, selectExp, cancellationToken);

                if (departmentDetailsDto != null)
                {
                    activity?.SetStatus(ActivityStatusCode.Ok, "Department retrieved successfully");
                    _logger.LogInformation("Department retrieved successfully. DepartmentId: {DepartmentId}, Name: {DepartmentName}", request.Id, departmentDetailsDto.Name);
                }
                else
                {
                    activity?.SetStatus(ActivityStatusCode.Error, "Department not found");
                    _logger.LogWarning("Department not found. DepartmentId: {DepartmentId}", request.Id);
                }

                return Result<DepartmentDetailsDto>.Success(departmentDetailsDto);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Error occurred while retrieving department. DepartmentId: {DepartmentId}", request.Id);
                return Result<DepartmentDetailsDto>.Failure("Exception", "An error occurred while retrieving the department.");
            }
        }
    }
}

public class DepartmentDetailsDto
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastModifiedAtUtc { get; set; }
}
