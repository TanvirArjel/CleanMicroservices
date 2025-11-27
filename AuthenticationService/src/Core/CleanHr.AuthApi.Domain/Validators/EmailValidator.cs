using System;
using System.Threading;
using System.Threading.Tasks;
using CleanHr.AuthApi.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;

namespace CleanHr.AuthApi.Domain.Validators;

public class EmailValidator : AbstractValidator<string>
{
    public EmailValidator(Guid userId, IApplicationUserRepository userRepository)
    {
        RuleFor(email => email)
            .NotEmpty()
            .WithMessage("The Email is required.")
            .EmailAddress()
            .WithMessage("The Email is not a valid email.")
            .MaximumLength(50)
            .WithMessage("The Email can't be more than 50 characters long.")
            .MustAsync((email, cancellationToken) => BeUniqueEmailAsync(email, userId, userRepository, cancellationToken))
            .WithMessage("A user already exists with the provided email.");
    }

    protected override bool PreValidate(ValidationContext<string> context, ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        if (context.InstanceToValidate == null)
        {
            result.Errors.Add(new ValidationFailure("Email", "The Email cannot be null."));
            return false;
        }

        return true;
    }

    private static async Task<bool> BeUniqueEmailAsync(
        string email,
        Guid userId,
        IApplicationUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        bool isEmailExistent = await userRepository.ExistsAsync(u => u.Email == email && u.Id != userId);
        return !isEmailExistent;
    }
}
