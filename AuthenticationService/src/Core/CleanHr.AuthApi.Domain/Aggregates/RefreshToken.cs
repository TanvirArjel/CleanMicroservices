using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CleanHr.AuthApi.Domain.Aggregates.Validators;
using FluentValidation.Results;

namespace CleanHr.AuthApi.Domain.Aggregates;

public class RefreshToken
{
    private RefreshToken(Guid userId, string token, int expirationDays = 30)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Token = token?.Trim();
        CreatedAtUtc = DateTime.UtcNow;
        ExpireAtUtc = DateTime.UtcNow.AddDays(expirationDays);
        IsRevoked = false;
    }

    // This is needed for EF Core query mapping
    [JsonConstructor]
    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Token { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime ExpireAtUtc { get; private set; }

    public bool IsRevoked { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    // Navigation properties
    public ApplicationUser ApplicationUser { get; private set; }

    /// <summary>
    /// Factory method for creating a new RefreshToken with validation.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="token">The refresh token.</param>
    /// <param name="expirationDays">Number of days until expiration (default 30).</param>
    /// <returns>Returns <see cref="Task{TResult}"/>.</returns>
    public static async Task<Result<RefreshToken>> CreateAsync(
        Guid userId,
        string token,
        int expirationDays = 30)
    {
        RefreshTokenValidator validator = new();

        RefreshToken refreshToken = new(userId, token, expirationDays);

        ValidationResult validationResult = await validator.ValidateAsync(refreshToken);

        if (validationResult.IsValid == false)
        {
            return Result<RefreshToken>.Failure(validationResult.ToDictionary());
        }

        return Result<RefreshToken>.Success(refreshToken);
    }

    public Result UpdateToken(string newToken, int expirationDays = 30)
    {
        if (string.IsNullOrWhiteSpace(newToken))
        {
            return Result.Failure("The token cannot be empty.");
        }

        Token = newToken.Trim();
        CreatedAtUtc = DateTime.UtcNow;
        ExpireAtUtc = DateTime.UtcNow.AddDays(expirationDays);

        return Result.Success();
    }

    public Result<bool> IsExpired()
    {
        bool isExpired = DateTime.UtcNow > ExpireAtUtc;
        return Result<bool>.Success(isExpired);
    }

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAtUtc = DateTime.UtcNow;
    }

    public bool IsValid()
    {
        return !IsRevoked && DateTime.UtcNow <= ExpireAtUtc;
    }
}
