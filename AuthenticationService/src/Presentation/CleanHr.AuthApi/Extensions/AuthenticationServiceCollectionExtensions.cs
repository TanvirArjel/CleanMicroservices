using System.Text;
using CleanHr.AuthApi.Application.Infrastructures;
using CleanHr.AuthApi.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using TanvirArjel.ArgumentChecker;
using CleanHr.AuthApi.Application.Services;

namespace CleanHr.AuthApi.Extensions;

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
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtConfig.Issuer,
                ValidAudience = jwtConfig.Issuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key))
            };
        });
    }

    public static void AddJwtTokenGenerator(this IServiceCollection services, JwtConfig jwtConfig)
    {
        services.ThrowIfNull(nameof(services));
        jwtConfig.ThrowIfNull(nameof(jwtConfig));

        services.AddSingleton(jwtConfig);
        services.AddScoped<JwtTokenManager>();
    }
    public static void AddExternalLogins(this IServiceCollection services, IConfiguration configuration)
    {
        services.ThrowIfNull(nameof(services));
        configuration.ThrowIfNull(nameof(configuration));

        var authBuilder = services.AddAuthentication();

        string googleClientId = configuration.GetSection("ExternalLoginProviders:Google:ClientId").Value;
        if (!string.IsNullOrWhiteSpace(googleClientId))
        {
            authBuilder.AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = googleClientId;
                googleOptions.ClientSecret = configuration.GetSection("ExternalLoginProviders:Google:ClientSecret").Value;
                googleOptions.SaveTokens = true;
            });
        }

        string twitterConsumerKey = configuration.GetSection("ExternalLoginProviders:Twitter:ConsumerKey").Value;
        if (!string.IsNullOrWhiteSpace(twitterConsumerKey))
        {
            authBuilder.AddTwitter(twitterOptions =>
            {
                twitterOptions.ConsumerKey = twitterConsumerKey;
                twitterOptions.ConsumerSecret = configuration.GetSection("ExternalLoginProviders:Twitter:ConsumerSecret").Value;
                twitterOptions.SaveTokens = true;
            });
        }
    }

    public static void AddAllHealthChecks(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ReadinessHealthCheck>();

        services.AddHealthChecks()
                .AddTypeActivatedCheck<DbConnectionHealthCheck>(
                    "Database",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { HealthCheckTags.Database },
                    args: new object[] { connectionString })
                .AddCheck<ReadinessHealthCheck>("Readiness", tags: new[] { HealthCheckTags.Ready });

        // This is has been disabled until add support for .NET 8.0 and EF Core 8.0
        // services.AddHealthChecksUI().AddInMemoryStorage();
    }
}
