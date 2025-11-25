using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CleanHr.AuthApi.Application.Commands;
using CleanHr.AuthApi.Application.Infrastructures;
using CleanHr.AuthApi.Application.Queries;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Aggregates;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TanvirArjel.ArgumentChecker;

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
    private readonly IMediator _mediator;

    public JwtTokenManager(
        JwtConfig jwtConfig,
        UserManager<ApplicationUser> userManager,
        IMediator mediator)
    {
        _jwtConfig = jwtConfig ?? throw new ArgumentNullException(nameof(jwtConfig));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public async Task<AuthenticationResult> GetTokenAsync(string userId)
    {
        userId.ThrowIfNullOrEmpty(nameof(userId));

        ApplicationUser user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        return await GetTokenAsync(user);
    }

    public async Task<AuthenticationResult> GetTokenAsync(string accessToken, string refreshToken)
    {
        accessToken.ThrowIfNull(nameof(accessToken));

        ClaimsPrincipal claimsPrincipal = ParseExpiredToken(accessToken);
        string userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        IsRefreshTokenValidQuery isRefreshTokenValidQuery = new(Guid.Parse(userId), refreshToken);

        bool isValid = await _mediator.Send(isRefreshTokenValidQuery);

        if (!isValid)
        {
            throw new SecurityTokenException("Invalid refresh token.");
        }

        ApplicationUser user = await _userManager.FindByIdAsync(userId);

        // SECURITY: Always rotate refresh token when used
        // This generates a new refresh token and invalidates the old one
        return await GetTokenAsync(user, rotateRefreshToken: true);
    }

    public async Task<AuthenticationResult> GetTokenAsync(ApplicationUser user, bool rotateRefreshToken = false)
    {
        ArgumentNullException.ThrowIfNull(user);

        IList<string> roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);

        GetRefreshTokenQuery getRefreshTokenQuery = new(user.Id);

        RefreshToken refreshToken = await _mediator.Send(getRefreshTokenQuery);

        // Generate new refresh token if:
        // 1. No refresh token exists
        // 2. Existing token is expired
        // 3. Token rotation is requested (during refresh flow)
        bool shouldGenerateNewToken = refreshToken == null ||
                                     refreshToken.ExpireAtUtc < DateTime.UtcNow ||
                                     rotateRefreshToken;

        if (shouldGenerateNewToken)
        {
            string token = GetRefreshToken();

            if (refreshToken == null)
            {
                // Create new refresh token for first-time login
                StoreRefreshTokenCommand storeRefreshTokenCommand = new(user.Id, token);
                Result<RefreshToken> storeResult = await _mediator.Send(storeRefreshTokenCommand);

                if (storeResult.IsSuccess == false)
                {
                    throw new InvalidOperationException("Failed to store refresh token.");
                }

                refreshToken = storeResult.Value;
            }
            else
            {
                // Update existing refresh token (rotation or expiration)
                UpdateRefreshTokenCommand updateRefreshTokenCommand = new(user.Id, token);
                Result<RefreshToken> updateResult = await _mediator.Send(updateRefreshTokenCommand);

                if (updateResult.IsSuccess == false)
                {
                    throw new InvalidOperationException("Failed to update refresh token.");
                }

                refreshToken = updateResult.Value;
            }
        }

        DateTime utcNow = DateTime.Now;

        string fullName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName;

        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, fullName),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sid, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, fullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, utcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)),
        ];

        if (roles != null && roles.Any())
        {
            foreach (string item in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, item));
            }
        }

        SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(_jwtConfig.Key));
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

        return new AuthenticationResult
        {
            AccessToken = newAccessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn = _jwtConfig.TokenLifeTime
        };
    }

    public ClaimsPrincipal ParseExpiredToken(string accessToken)
    {
        TokenValidationParameters tokenValidationParameters = new()
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key)),
            ValidateLifetime = true
        };

        JwtSecurityTokenHandler tokenHandler = new();
        ClaimsPrincipal principal = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenException("Invalid access token.");
        }

        return principal;
    }

    private string GetRefreshToken()
    {
        byte[] randomNumber = new byte[32];
        using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
        randomNumberGenerator.GetBytes(randomNumber);

        string refreshToken = Convert.ToBase64String(randomNumber);

        return refreshToken;
    }
}
