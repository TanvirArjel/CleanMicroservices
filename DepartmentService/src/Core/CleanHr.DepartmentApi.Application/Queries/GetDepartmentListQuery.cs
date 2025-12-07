using System.Diagnostics;
using CleanHr.DepartmentApi.Application.Caching.Repositories;
using CleanHr.DepartmentApi.Application.Constants;
using MediatR;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.DepartmentApi.Application.Queries;

public sealed class GetDepartmentListQuery : IRequest<List<DepartmentDto>>
{
    private class GetDepartmentListQueryHandler : IRequestHandler<GetDepartmentListQuery, List<DepartmentDto>>
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

        public async Task<List<DepartmentDto>> Handle(GetDepartmentListQuery request, CancellationToken cancellationToken)
        {
            using var activity = ApplicationActivityConstants.Source.StartActivity(
                "GetDepartmentList",
                ActivityKind.Internal);
            try
            {
                request.ThrowIfNull(nameof(request));
                List<DepartmentDto> departmentDtos = await _departmentCacheRepository.GetListAsync();
                return departmentDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while handling GetDepartmentListQuery");
                throw;
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
