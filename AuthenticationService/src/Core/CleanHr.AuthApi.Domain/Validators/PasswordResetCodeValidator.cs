using System;
using CleanHr.AuthApi.Domain.Models;
using FluentValidation;

namespace CleanHr.AuthApi.Domain.Validators;

internal class PasswordResetCodeValidator : AbstractValidator<PasswordResetCode>
{
    public PasswordResetCodeValidator()
    {
        RuleFor(p => p.UserId)
            .NotEmpty()
            .WithMessage("The UserId is required.");

        RuleFor(p => p.Email)
            .NotNull()
            .WithMessage("The Email is required.")
            .SetValidator(new EmailValidator());

        RuleFor(p => p.Code)
            .NotNull()
            .WithMessage("The Code is required.")
            .SetValidator(new CodeValidator());

        RuleFor(p => p.SentAtUtc)
            .NotEmpty()
            .WithMessage("The SentAtUtc is required.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .WithMessage("The SentAtUtc cannot be in the future.");
    }
}
