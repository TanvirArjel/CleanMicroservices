using System;
using System.Threading;
using System.Threading.Tasks;
using CleanHr.AuthApi.Domain.Repositories;
using FluentValidation;

namespace CleanHr.AuthApi.Domain.Validators;

public class EmailValidator : AbstractValidator<string>
{
    public EmailValidator(Guid userId = default, IApplicationUserRepository userRepository = null)
    {
        RuleFor(email => email)
            .NotEmpty()
            .WithMessage("The Email is required.")
            .EmailAddress()
            .WithMessage("The Email is not a valid email.")
            .MaximumLength(50)
            .WithMessage("The Email can't be more than 50 characters long.");

        if (userRepository != null)
        {
            RuleFor(email => email)
                .MustAsync((email, cancellationToken) => BeUniqueEmailAsync(email, userId, userRepository, cancellationToken))
                .WithMessage("A user already exists with the provided email.");
        }
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
