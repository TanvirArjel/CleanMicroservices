using System;
using System.Threading;
using System.Threading.Tasks;
using CleanHr.AuthApi.Domain.Repositories;
using FluentValidation;

namespace CleanHr.AuthApi.Domain.Validators;

internal class UniqueEmailValidator : AbstractValidator<string>
{
    public UniqueEmailValidator(Guid userId, IApplicationUserRepository userRepository)
    {
        RuleFor(email => email)
            .MustAsync((email, cancellationToken) => BeUniqueEmailAsync(email, userId, userRepository, cancellationToken))
            .WithMessage("A user already exists with the provided email.");
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
