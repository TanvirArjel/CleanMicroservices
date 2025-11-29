using System;
using System.Threading;
using System.Threading.Tasks;
using CleanHr.AuthApi.Domain.Models;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace CleanHr.AuthApi.Domain.Validators;

internal class EmailVerificationCodeValidator : AbstractValidator<EmailVerificationCode>
{
    private readonly UserManager<ApplicationUser> _userManager;
    public EmailVerificationCodeValidator(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;

        RuleFor(e => e.Email)
            .SetValidator(new EmailValidator())
            .CustomAsync(ValidateEmailAsync);

        RuleFor(e => e.Code)
            .SetValidator(new CodeValidator());

        RuleFor(e => e.SentAtUtc)
            .NotEmpty()
            .WithMessage("The SentAtUtc is required.")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("The SentAtUtc cannot be in the future.");
    }

    private async Task ValidateEmailAsync(
        string email,
        ValidationContext<EmailVerificationCode> context,
        CancellationToken cancellationToken)
    {
        ApplicationUser applicationUser = await _userManager.FindByEmailAsync(email);

        if (applicationUser == null)
        {
            context.AddFailure("Email", "The provided email is not related to any account.");
            return;
        }

        if (applicationUser.EmailConfirmed)
        {
            context.AddFailure("Email", "The email is already confirmed.");
            return;
        }
    }
}
