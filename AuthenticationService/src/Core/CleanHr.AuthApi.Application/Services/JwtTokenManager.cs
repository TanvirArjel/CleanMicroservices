using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CleanHr.AuthApi.Application.Extensions;
using CleanHr.AuthApi.Application.Infrastructures;
using CleanHr.AuthApi.Application.Queries;
using CleanHr.AuthApi.Application.Telemetry;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Services;

public class AuthenticationResult
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
}

public class JwtTokenManager
{
    private readonly JwtConfig _jwtConfig;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository _repository;
    private readonly IMediator _mediator;
    private readonly ILogger<JwtTokenManager> _logger;

    public JwtTokenManager(
        JwtConfig jwtConfig,
        UserManager<ApplicationUser> userManager,
        IRepository repository,
        IMediator mediator,
        ILogger<JwtTokenManager> logger)
    {
        _jwtConfig = jwtConfig ?? throw new ArgumentNullException(nameof(jwtConfig));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AuthenticationResult>> GetTokenAsync(string userId)
    {
        using var activity = ApplicationActivityConstants.Source.StartActivity("GetJwtToken", ActivityKind.Internal);
        activity?.SetTag("user.id", userId);

        if (string.IsNullOrWhiteSpace(userId))
        {
            activity.SetStatus(ActivityStatusCode.Error, "UserId is null or empty");
            _logger.LogWarning("UserId is null or empty in GetTokenAsync");
            return Result<AuthenticationResult>.Failure("UserId cannot be null or empty.");
        }

        ApplicationUser user = await _userManager.FindByIdAsync(userId);

        var result = await GetTokenAsync(user);

        if (result.IsSuccess)
        {
            activity.SetStatus(ActivityStatusCode.Ok, "Token generated successfully");
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Error, "Failed to generate token");
        }

        return result;
    }

    public async Task<Result<AuthenticationResult>> GetTokenAsync(ApplicationUser user)
    {
        using var activity = ApplicationActivityConstants.Source.StartActivity("GetJwtToken", ActivityKind.Internal);
        activity?.SetTag("user.id", user?.Id.ToString());

        var result = await GetTokenAsync(user, oldRefreshToken: null);

        if (result.IsSuccess)
        {
            activity.SetStatus(ActivityStatusCode.Ok, "Token generated successfully");
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Error, "Failed to generate token");
        }

        return result;
    }

    public async Task<Result<AuthenticationResult>> GetTokenAsync(string accessToken, string refreshToken)
    {
        using var activity = ApplicationActivityConstants.Source.StartActivity(
               "GenerateJwtToken",
               ActivityKind.Internal);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogWarning("Access token is null or empty in GetTokenAsync");
            return Result<AuthenticationResult>.Failure("Access token cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Refresh token is null or empty in GetTokenAsync");
            return Result<AuthenticationResult>.Failure("Refresh token cannot be null or empty.");
        }

        ClaimsPrincipal claimsPrincipal = ParseExpiredToken(accessToken);
        string userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        IsRefreshTokenValidQuery isRefreshTokenValidQuery = new(Guid.Parse(userId), refreshToken);

        bool isValid = await _mediator.Send(isRefreshTokenValidQuery);

        if (!isValid)
        {
            throw new SecurityTokenException("Invalid refresh token.");
        }

        ApplicationUser user = await _userManager.FindByIdAsync(userId);

        var result = await GetTokenAsync(user, oldRefreshToken: refreshToken);

        if (result.IsSuccess)
        {
            activity.SetStatus(ActivityStatusCode.Ok, "Token generated successfully");
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Error, "Failed to generate token");
        }

        return result;
    }

    private async Task<Result<AuthenticationResult>> GetTokenAsync(ApplicationUser user, string oldRefreshToken)
    {
        using var _loggerScope = _logger.BeginScope(new Dictionary<string, object>
        {
            { "UserId", user?.Id.ToString() },
            {"Email", user?.Email },
            {"UserName", user?.UserName }
        });

        try
        {
            if (user == null)
            {
                _logger.LogWarning("User is null in GetTokenAsync");
                return Result<AuthenticationResult>.Failure("User cannot be null.");
            }

            RefreshToken refreshToken;

            if (oldRefreshToken != null)
            {
                Result<RefreshToken> updateResult = await RevokeOldAndCreateNewTokenAsync(
                    user.Id,
                    oldRefreshToken,
                    CancellationToken.None);

                refreshToken = updateResult.Value;

                if (updateResult.IsSuccess == false)
                {
                    _logger.LogWarning("Failed to rotate refresh token for user {UserId}", user.Id);
                    return Result<AuthenticationResult>.Failure(updateResult.Errors);
                }
            }
            else
            {
                // First-time login: create new refresh token
                Result<RefreshToken> storeResult = await CreateNewRefreshTokenAsync(
                    user.Id,
                    CancellationToken.None);

                if (storeResult.IsSuccess == false)
                {
                    _logger.LogWarning("Failed to create new refresh token for user {UserId}", user.Id);
                    return Result<AuthenticationResult>.Failure(storeResult.Errors);
                }

                refreshToken = storeResult.Value;
            }

            DateTime utcNow = DateTime.UtcNow;

            List<Claim> claims =
            [
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sid, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.GivenName, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                // NOTE: iat (issued at), nbf (not before), and exp (expires) are automatically added by JwtSecurityToken
            ];

            IList<string> roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);

            if (roles != null && roles.Any())
            {
                foreach (string item in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, item));
                }
            }

            _logger.LogDebug("Generating JWT token for user {UserId}", user.Id);

            const string SymmetricKeyId = "MyAppSharedSecretKey";
            SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(_jwtConfig.Key))
            {
                KeyId = SymmetricKeyId
            };

            SigningCredentials signingCredentials = new(signingKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken jwt = new(
                signingCredentials: signingCredentials,
                claims: claims,
                notBefore: utcNow,
                expires: utcNow.AddSeconds(_jwtConfig.TokenLifeTime),
                audience: _jwtConfig.Issuer,
                issuer: _jwtConfig.Issuer);

            JwtSecurityTokenHandler jwtSecurityTokenHandler = new();
            jwtSecurityTokenHandler.OutboundClaimTypeMap.Clear();
            string newAccessToken = jwtSecurityTokenHandler.WriteToken(jwt);

            _logger.LogDebug("JWT token generated successfully for user {UserId}", user.Id);

            return Result<AuthenticationResult>.Success(new AuthenticationResult
            {
                AccessToken = newAccessToken,
                RefreshToken = refreshToken.Token,
                ExpiresIn = _jwtConfig.TokenLifeTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while generating JWT token for user {UserId}", user?.Id);
            return Result<AuthenticationResult>.Failure("Exception while generating JWT token.");
        }
    }

    private ClaimsPrincipal ParseExpiredToken(string accessToken)
    {
#pragma warning disable CA5404 // Do not disable token validation checks
        TokenValidationParameters tokenValidationParameters = new()
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtConfig.Issuer,
            ValidAudience = _jwtConfig.Issuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key)),
            // SECURITY: Set to false because we're explicitly parsing EXPIRED tokens during refresh flow
            // The refresh token validation provides the security check, not the expired access token
            ValidateLifetime = false
        };
#pragma warning restore CA5404 // Do not disable token validation checks

        JwtSecurityTokenHandler tokenHandler = new();
        ClaimsPrincipal principal = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenException("Invalid access token.");
        }

        return principal;
    }

    private async Task<Result<RefreshToken>> RevokeOldAndCreateNewTokenAsync(
        Guid userId,
        string oldToken,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rotating refresh token for user {UserId}", userId);

        // Find the old refresh token
        RefreshToken oldRefreshToken = await _repository.GetAsync<RefreshToken>(
            rt => rt.UserId == userId && rt.Token == oldToken,
            cancellationToken);

        if (oldRefreshToken == null)
        {
            _logger.LogWarning("Refresh token not found for user {UserId}", userId);
            return Result<RefreshToken>.Failure($"The RefreshToken does not exist for user: {userId}.");
        }

        // SECURITY: Detect token reuse attack with device isolation
        // If token has already been used, this is a security breach - revoke only this device's token family
        if (oldRefreshToken.IsUsed())
        {
            _logger.LogWarning(
                "SECURITY ALERT: Refresh token reuse detected for user {UserId}. Token {TokenId} (Family: {TokenFamilyId}) was already used at {UsedAt}. Revoking token family.",
                userId,
                oldRefreshToken.Id,
                oldRefreshToken.TokenFamilyId,
                oldRefreshToken.UsedAtUtc);

            // Revoke only tokens in the same family (device isolation - don't affect other devices)
            List<RefreshToken> familyTokens = await _repository.GetListAsync<RefreshToken>(
                rt => rt.TokenFamilyId == oldRefreshToken.TokenFamilyId && !rt.RevokedAtUtc.HasValue,
                cancellationToken);

            foreach (RefreshToken token in familyTokens)
            {
                token.Revoke();
                _repository.Update(token);
            }

            await _repository.SaveChangesAsync(cancellationToken);

            return Result<RefreshToken>.Failure("Token reuse detected. This device's session has been revoked for security. Please login again.");
        }

        // Mark old token as used (one-time use enforcement)
        oldRefreshToken.MarkAsUsed();

        // Also revoke it for extra safety
        oldRefreshToken.Revoke();
        _repository.Update(oldRefreshToken);

        // Create new refresh token in the SAME family (token rotation within device/session)
        string newToken = GenerateRefreshToken();
        Result<RefreshToken> createResult = await RefreshToken.CreateAsync(
            userId,
            newToken,
            oldRefreshToken.TokenFamilyId); // Inherit family ID for rotation chain

        if (createResult.IsSuccess == false)
        {
            _logger.LogWarning("Failed to create new refresh token for user {UserId}", userId);
            return createResult;
        }

        RefreshToken newRefreshToken = createResult.Value;
        _repository.Add(newRefreshToken);

        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Refresh token rotated successfully for user {UserId}", userId);

        return Result<RefreshToken>.Success(newRefreshToken);
    }

    private async Task<Result<RefreshToken>> CreateNewRefreshTokenAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new refresh token for user {UserId}", userId);
        string token = GenerateRefreshToken();
        Result<RefreshToken> result = await RefreshToken.CreateAsync(userId, token);

        if (result.IsSuccess == false)
        {
            _logger.LogWarning("Failed to create new refresh token for user {UserId}", userId);
            return result;
        }

        RefreshToken refreshToken = result.Value;

        _repository.Add(refreshToken);
        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("New refresh token created successfully for user {UserId}", userId);

        return Result<RefreshToken>.Success(refreshToken);
    }

    private string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];
        using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
        randomNumberGenerator.GetBytes(randomNumber);

        string refreshToken = Convert.ToBase64String(randomNumber);

        return refreshToken;
    }
}
