using System.Diagnostics;
using CleanHr.DepartmentApi.Application.Caching.Handlers;
using CleanHr.DepartmentApi.Application.Constants;
using CleanHr.DepartmentApi.Domain;
using CleanHr.DepartmentApi.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.DepartmentApi.Application.Commands;

public sealed class DeleteDepartmentCommand : IRequest<Result>
{
    public DeleteDepartmentCommand(Guid departmentId)
    {
        Id = departmentId;
    }

    public Guid Id { get; }

    private class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand, Result>
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IDepartmentCacheHandler _departmentCacheHandler;
        private readonly ILogger<DeleteDepartmentCommandHandler> _logger;

        public DeleteDepartmentCommandHandler(
            IDepartmentRepository departmentRepository,
            IDepartmentCacheHandler departmentCacheHandler,
            ILogger<DeleteDepartmentCommandHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(departmentRepository);
            ArgumentNullException.ThrowIfNull(departmentCacheHandler);
            ArgumentNullException.ThrowIfNull(logger);

            _departmentRepository = departmentRepository;
            _departmentCacheHandler = departmentCacheHandler;
            _logger = logger;
        }

        public async Task<Result> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
        {
            using var activity = ApplicationActivityConstants.Source.StartActivity(
                "DeleteDepartmentCommand.Handle",
                ActivityKind.Internal);
            activity?.SetTag("departmentId", request.Id);

            try
            {
                _logger.LogInformation("Deleting department with ID: {DepartmentId}", request.Id);
                _ = request.ThrowIfNull(nameof(request));

                var departmentResult = await _departmentRepository.GetByIdAsync(request.Id, cancellationToken);

                if (departmentResult.IsSuccess == false)
                {
                    return departmentResult;
                }

                if (departmentResult.Value == null)
                {
                    _logger.LogWarning("Department with ID: {DepartmentId} not found.", request.Id);
                    return Result.Failure("DepartmentId", "Department not found with the given ID.");
                }

                var department = departmentResult.Value;
                var deletionResult = await _departmentRepository.DeleteAsync(department, cancellationToken);

                if (deletionResult.IsSuccess == false)
                {
                    _logger.LogWarning("Failed to delete department with ID: {DepartmentId}.", request.Id);
                    return deletionResult;
                }

                await _departmentCacheHandler.RemoveListAsync();
                activity?.SetStatus(deletionResult.IsSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
                _logger.LogInformation("Deleted department with ID: {DepartmentId}", request.Id);
                return deletionResult;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Error occurred while deleting department with ID: {DepartmentId}", request.Id);
                return Result.Failure("Exception", "An error occurred while deleting the department.");
            }
        }
    }
}