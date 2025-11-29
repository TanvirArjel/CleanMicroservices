using System;
using FluentValidation;
using FluentValidation.Results;

namespace CleanHr.AuthApi.Domain.Validators;

internal class CodeValidator : AbstractValidator<string>
{
    public CodeValidator()
    {
        RuleFor(code => code)
            .NotEmpty()
            .WithMessage("The Code is required.")
            .Length(6)
            .WithMessage("The Code must be exactly 6 characters long.")
            .Matches("^[0-9]{6}$")
            .WithMessage("The Code must contain only digits.");
    }

    protected override bool PreValidate(ValidationContext<string> context, ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        if (context.InstanceToValidate == null)
        {
            result.Errors.Add(new ValidationFailure("Code", "The Code cannot be null."));
            return false;
        }

        return true;
    }
}
