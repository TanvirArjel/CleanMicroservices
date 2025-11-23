using System;
using System.Threading.Tasks;
using FluentValidation;

namespace CleanHr.EmployeeApi.Domain.Aggregates.Validators;

public class EmployeeDepartmentValidator : AbstractValidator<Guid>
{
    private readonly IDepartmentServiceClient _departmentServiceClient;

    public EmployeeDepartmentValidator(IDepartmentServiceClient departmentServiceClient)
    {
        _departmentServiceClient = departmentServiceClient ?? throw new ArgumentNullException(nameof(departmentServiceClient));

        // DepartmentId validation
        RuleFor(departmentId => departmentId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("The {PropertyName} is required.")
            .MustAsync(async (departmentId, cancellation) => await _departmentServiceClient.IsDepartmentExistentAsync(departmentId, cancellation))
            .WithMessage("The department with id {PropertyValue} does not exist.");
    }
}
