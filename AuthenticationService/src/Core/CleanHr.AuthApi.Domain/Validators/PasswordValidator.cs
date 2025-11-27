using System;
using FluentValidation;
using FluentValidation.Results;

namespace CleanHr.AuthApi.Domain.Validators;

public class PasswordValidator : AbstractValidator<string>
{
    public PasswordValidator()
    {
        RuleFor(password => password)
            .NotEmpty()
            .WithMessage("The Password is required.")
            .MinimumLength(8)
            .WithMessage("The Password must be at least 8 characters long.")
            .MaximumLength(20)
            .WithMessage("The Password cannot be more than 20 characters.");
    }

    protected override bool PreValidate(ValidationContext<string> context, ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        if (context.InstanceToValidate == null)
        {
            result.Errors.Add(new ValidationFailure("Password", "The Password cannot be null."));
            return false;
        }

        return true;
    }
}
