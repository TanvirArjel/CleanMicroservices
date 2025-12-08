using System;
using System.Threading;
using System.Threading.Tasks;
using CleanHr.DepartmentApi.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;

namespace CleanHr.DepartmentApi.Domain.Validators;

public class DepartmentNameValidator : AbstractValidator<string>
{
    private readonly IDepartmentRepository _departmentRepository;

    public DepartmentNameValidator(IDepartmentRepository departmentRepository, Guid departmentId)
    {
        _departmentRepository = departmentRepository;

        // Add validation rules here
        RuleFor(name => name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("The DepartmentName cannot be empty.")
                .MinimumLength(2)
                .WithMessage("The DepartmentName must be at least {MinLength} characters.")
                .MaximumLength(20)
                .WithMessage("The DepartmentName can't be more than {MaxLength} characters.")
                .MustAsync(async (name, token) => await IsUniqueNameAsync(departmentId, name, token))
                .WithMessage("The DepartmentName is already existent.");
    }

    protected override bool PreValidate(ValidationContext<string> context, ValidationResult result)
    {
        if (context?.InstanceToValidate == null)
        {
            result?.Errors.Add(new ValidationFailure(nameof(context.InstanceToValidate), "The DepartmentName cannot be null."));
            return false;
        }

        return true;
    }

    protected async Task<bool> IsUniqueNameAsync(Guid id, string name, CancellationToken cancellationToken)
    {
        Result<bool> existsResult = await _departmentRepository.ExistsAsync(d => d.Name == name && d.Id != id, cancellationToken);
        return existsResult.IsSuccess && !existsResult.Value;
    }
}
