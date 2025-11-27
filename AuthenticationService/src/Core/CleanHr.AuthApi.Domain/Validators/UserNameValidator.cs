using System;
using System.Threading;
using System.Threading.Tasks;
using CleanHr.AuthApi.Domain.Repositories;
using FluentValidation;

namespace CleanHr.AuthApi.Domain.Validators;

public class UserNameValidator : AbstractValidator<string>
{
    public UserNameValidator(Guid userId = default, IApplicationUserRepository userRepository = null)
    {
        RuleFor(userName => userName)
            .NotEmpty()
            .WithMessage("The UserName is required.")
            .MinimumLength(5)
            .WithMessage("The UserName must be at least 5 characters.")
            .MaximumLength(50)
            .WithMessage("The UserName can't be more than 50 characters long.");

        if (userRepository != null)
        {
            RuleFor(userName => userName)
                .MustAsync((userName, cancellationToken) => BeUniqueUserNameAsync(userName, userId, userRepository, cancellationToken))
                .WithMessage("A user already exists with the provided username.");
        }
    }

    private static async Task<bool> BeUniqueUserNameAsync(
        string userName,
        Guid userId,
        IApplicationUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        bool isUserNameExistent = await userRepository.ExistsAsync(u => u.UserName == userName && u.Id != userId);
        return !isUserNameExistent;
    }
}
