using System.Text;
using CleanHr.DepartmentApi.Configs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.DepartmentApi.Extensions;

internal static class AuthenticationServiceCollectionExtensions
{
    public static void AddJwtAuthentication(this IServiceCollection services, JwtConfig jwtConfig)
    {
        services.ThrowIfNull(nameof(services));
        jwtConfig.ThrowIfNull(nameof(jwtConfig));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            const string SymmetricKeyId = "MyAppSharedSecretKey";
            SymmetricSecurityKey validationKey = new(Encoding.UTF8.GetBytes(jwtConfig.Key))
            {
                KeyId = SymmetricKeyId
            };

            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = jwtConfig.Issuer,
                ValidAudience = jwtConfig.Issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = validationKey
            };
        });
    }

}
