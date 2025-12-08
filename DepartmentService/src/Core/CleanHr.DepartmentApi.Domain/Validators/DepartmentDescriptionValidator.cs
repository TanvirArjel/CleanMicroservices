using FluentValidation;
using FluentValidation.Results;

namespace CleanHr.DepartmentApi.Domain.Validators;

public class DepartmentDescriptionValidator : AbstractValidator<string>
{
    public DepartmentDescriptionValidator()
    {
        RuleFor(description => description)
               .Cascade(CascadeMode.Stop)
               .NotEmpty()
               .WithMessage("The Description cannot be empty.")
               .MinimumLength(20)
               .WithMessage("The Description must be at least 20 characters.")
               .MaximumLength(200)
               .WithMessage("The Description can't be more than 200 characters.");
    }

    protected override bool PreValidate(ValidationContext<string> context, ValidationResult result)
    {
        if (context?.InstanceToValidate == null)
        {
            result?.Errors.Add(new ValidationFailure(nameof(context.InstanceToValidate), "The Description is required."));
            return false;
        }

        return true;
    }
}
