using System;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;

namespace CleanHr.AuthApi.Domain.Validators;

internal class PasswordValidator : AbstractValidator<string>
{
    public PasswordValidator()
    {
        RuleFor(password => password)
            .NotEmpty()
            .WithMessage("The Password is required.")
            .MinimumLength(8)
            .WithMessage("The Password must be at least 8 characters long.")
            .MaximumLength(20)
            .WithMessage("The Password cannot be more than 20 characters.")
            .Must(password => !password.Contains(' ', StringComparison.Ordinal))
            .WithMessage("The Password cannot contain whitespace.")
            .Must(ContainsOnlyAllowedCharacters)
            .WithMessage("The Password can only contain lowercase letters, uppercase letters, digits, and special characters.");
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

    private static bool ContainsOnlyAllowedCharacters(string password)
    {
        string specialCharacters = "!@#$%^&*()_+-=[]{}|;:',.<>?/~`";
        return password.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || specialCharacters.Contains(c, StringComparison.Ordinal));
    }
}
