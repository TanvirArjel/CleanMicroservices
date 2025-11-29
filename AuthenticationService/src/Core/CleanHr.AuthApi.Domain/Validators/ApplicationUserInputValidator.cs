using System;
using CleanHr.AuthApi.Domain.Models;
using CleanHr.AuthApi.Domain.Repositories;
using FluentValidation;

namespace CleanHr.AuthApi.Domain.Validators;

internal record ApplicationUserInput(string Password, string Email, string UserName);

internal class ApplicationUserInputValidator : AbstractValidator<ApplicationUserInput>
{
    public ApplicationUserInputValidator(Guid userId, IApplicationUserRepository repository)
    {
        RuleFor(x => x.Password)
            .NotNull()
            .WithMessage("The Password cannot be null.")
            .SetValidator(new PasswordValidator());

        RuleFor(x => x.Email)
            .NotNull()
            .WithMessage("The Email cannot be null.")
            .SetValidator(new EmailValidator())
            .SetValidator(new UniqueEmailValidator(userId, repository));

        RuleFor(x => x.UserName)
            .NotNull()
            .WithMessage("The UserName cannot be null.")
            .SetValidator(new UserNameValidator(userId, repository));
    }
}
