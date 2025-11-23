using CleanHr.EmployeeApi.Application.Caching.Handlers;
using CleanHr.EmployeeApi.Domain;
using CleanHr.EmployeeApi.Domain.Aggregates;
using MediatR;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.EmployeeApi.Application.Commands;

public record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    Guid DepartmentId,
    DateTime DateOfBirth,
    string Email,
    string PhoneNumber) : IRequest<Result<Guid>>;

internal class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentServiceClient _departmentServiceClient;
    private readonly IEmployeeCacheHandler _employeeCacheHandler;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IDepartmentServiceClient departmentServiceClient,
        IEmployeeCacheHandler employeeCacheHandler)
    {
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _departmentServiceClient = departmentServiceClient ?? throw new ArgumentNullException(nameof(departmentServiceClient));
        _employeeCacheHandler = employeeCacheHandler ?? throw new ArgumentNullException(nameof(employeeCacheHandler));
    }

    public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        request.ThrowIfNull(nameof(request));

        Result<Employee> result = await Employee.CreateAsync(
            _departmentServiceClient,
            _employeeRepository,
            request.FirstName,
            request.LastName,
            request.DepartmentId,
            request.DateOfBirth,
            request.Email,
            request.PhoneNumber);

        if (result.IsSuccess == false)
        {
            return Result<Guid>.Failure(result.Errors);
        }

        // Persist to the database
        await _employeeRepository.InsertAsync(result.Value);

        return Result<Guid>.Success(result.Value.Id);
    }
}