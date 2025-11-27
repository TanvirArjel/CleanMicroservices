using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CleanHr.AuthApi.Domain.Aggregates.Validators;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;

namespace CleanHr.AuthApi.Domain.Aggregates;

public class ApplicationUser : IdentityUser<Guid>
{
    // Private constructor for new instances
    private ApplicationUser(Guid id)
    {
        Id = id;
        IsDisabled = false;
        EmailConfirmed = false;
        PhoneNumberConfirmed = false;
        TwoFactorEnabled = false;
        LockoutEnabled = true;
        AccessFailedCount = 0;
    }

    // This is needed for EF Core query mapping and Identity framework
    [JsonConstructor]
    public ApplicationUser()
    {
    }

    public bool IsDisabled { get; private set; }

    public DateTime? LastLoggedInAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; private set; }

    // Navigation Properties
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];

    /// <summary>
    /// Factory method for creating a new ApplicationUser with validation.
    /// </summary>
    /// <param name="repository">The instance of <see cref="IApplicationUserRepository"/>.</param>
    /// <param name="email">The email address.</param>
    /// <param name="password">The password.</param>
    /// <param name="userName">The username (optional, defaults to email if not provided).</param>
    /// <returns>Returns <see cref="Task{TResult}"/>.</returns>
    public static async Task<Result<ApplicationUser>> CreateAsync(
        IApplicationUserRepository repository,
        string email,
        string password,
        string userName = null)
    {
        ArgumentNullException.ThrowIfNull(repository);

        // Trim inputs before validation
        string trimmedEmail = email?.Trim();
        string trimmedUserName = userName?.Trim();

        // Validate password
        PasswordValidator passwordValidator = new();
        ValidationResult passwordValidationResult = await passwordValidator.ValidateAsync(password);
        if (passwordValidationResult.IsValid == false)
        {
            return Result<ApplicationUser>.Failure(passwordValidationResult.ToDictionary());
        }

        Guid userId = Guid.NewGuid();

        EmailValidator emailValidator = new(userId, repository);
        ValidationResult emailResult = await emailValidator.ValidateAsync(trimmedEmail);
        if (emailResult.IsValid == false)
        {
            return Result<ApplicationUser>.Failure(emailResult.ToDictionary());
        }

        UserNameValidator userNameValidator = new(userId, repository);
        ValidationResult userNameResult = await userNameValidator.ValidateAsync(trimmedUserName ?? trimmedEmail);
        if (userNameResult.IsValid == false)
        {
            return Result<ApplicationUser>.Failure(userNameResult.ToDictionary());
        }

        ApplicationUser user = new(userId)
        {
            Email = trimmedEmail,
            UserName = trimmedUserName ?? trimmedEmail,
            NormalizedEmail = trimmedEmail?.ToUpperInvariant(),
            NormalizedUserName = (trimmedUserName ?? trimmedEmail)?.ToUpperInvariant()
        };

        return Result<ApplicationUser>.Success(user);
    }

    public async Task<Result> SetEmailAsync(IApplicationUserRepository repository, string email)
    {
        ArgumentNullException.ThrowIfNull(repository);

        // Trim input before validation
        string trimmedEmail = email?.Trim();

        EmailValidator emailValidator = new(Id, repository);
        ValidationResult emailResult = await emailValidator.ValidateAsync(trimmedEmail);

        if (emailResult.IsValid == false)
        {
            return Result.Failure(emailResult.ToDictionary());
        }

        Email = trimmedEmail;
        NormalizedEmail = trimmedEmail?.ToUpperInvariant();
        EmailConfirmed = false; // Reset email confirmation when email changes

        return Result.Success();
    }

    public Result Disable()
    {
        if (IsDisabled)
        {
            return Result.Failure("User is already disabled.");
        }

        IsDisabled = true;
        return Result.Success();
    }

    public Result Enable()
    {
        if (!IsDisabled)
        {
            return Result.Failure("User is already enabled.");
        }

        IsDisabled = false;
        return Result.Success();
    }

    public Result RecordLogin()
    {
        LastLoggedInAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public async Task<Result> SetPasswordAsync(string password, IPasswordHasher<ApplicationUser> passwordHasher)
    {
        ArgumentNullException.ThrowIfNull(passwordHasher);

        // Validate password
        PasswordValidator passwordValidator = new();
        ValidationResult passwordValidationResult = await passwordValidator.ValidateAsync(password);

        if (passwordValidationResult.IsValid == false)
        {
            return Result.Failure(passwordValidationResult.ToDictionary());
        }

        // Hash and set the password
        string newHashedPassword = passwordHasher.HashPassword(this, password);
        PasswordHash = newHashedPassword;

        return Result.Success();
    }
}
