using System;
using CleanHr.AuthApi.Domain.Models;
using FluentValidation;

namespace CleanHr.AuthApi.Domain.Validators;

public class PasswordResetCodeValidator : AbstractValidator<PasswordResetCode>
{
    public PasswordResetCodeValidator()
    {
        RuleFor(p => p.Email)
            .NotEmpty()
            .WithMessage("The Email is required.")
            .EmailAddress()
            .WithMessage("The Email is not a valid email.")
            .MaximumLength(50)
            .WithMessage("The Email can't be more than 50 characters long.");

        RuleFor(p => p.Code)
            .NotEmpty()
            .WithMessage("The Code is required.")
            .Length(6)
            .WithMessage("The Code must be exactly 6 characters long.")
            .Matches("^[0-9]{6}$")
            .WithMessage("The Code must contain only digits.");

        RuleFor(p => p.SentAtUtc)
            .NotEmpty()
            .WithMessage("The SentAtUtc is required.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .WithMessage("The SentAtUtc cannot be in the future.");
    }
}
