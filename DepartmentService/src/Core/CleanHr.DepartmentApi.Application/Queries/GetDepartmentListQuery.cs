using System.Diagnostics;
using CleanHr.DepartmentApi.Application.Caching.Repositories;
using CleanHr.DepartmentApi.Application.Constants;
using CleanHr.DepartmentApi.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.DepartmentApi.Application.Queries;

public sealed class GetDepartmentListQuery : IRequest<Result<List<DepartmentDto>>>
{
    private class GetDepartmentListQueryHandler : IRequestHandler<GetDepartmentListQuery, Result<List<DepartmentDto>>>
    {
        private readonly IDepartmentCacheRepository _departmentCacheRepository;
        private readonly ILogger<GetDepartmentListQueryHandler> _logger;

        public GetDepartmentListQueryHandler(
            IDepartmentCacheRepository departmentCacheRepository,
            ILogger<GetDepartmentListQueryHandler> logger)
        {
            _departmentCacheRepository = departmentCacheRepository ?? throw new ArgumentNullException(nameof(departmentCacheRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<List<DepartmentDto>>> Handle(
            GetDepartmentListQuery request,
            CancellationToken cancellationToken)
        {
            using var activity = ApplicationActivityConstants.Source.StartActivity(
                "GetDepartmentList",
                ActivityKind.Internal);
            try
            {
                request.ThrowIfNull(nameof(request));
                List<DepartmentDto> departmentDtos = await _departmentCacheRepository.GetListAsync();

                activity?.SetStatus(ActivityStatusCode.Ok, "Department list retrieved successfully");
                _logger.LogInformation("Department list retrieved successfully. Total Departments: {DepartmentCount}", departmentDtos.Count);
                return Result<List<DepartmentDto>>.Success(departmentDtos);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Error occurred while handling GetDepartmentListQuery");
                return Result<List<DepartmentDto>>.Failure("Exception", "An error occurred while retrieving the department list.");
            }
        }
    }
}

public class DepartmentDto
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastModifiedAtUtc { get; set; }
}
