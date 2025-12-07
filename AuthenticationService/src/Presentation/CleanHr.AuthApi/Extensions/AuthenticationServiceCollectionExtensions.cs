using System.Text;
using CleanHr.AuthApi.Application.Infrastructures;
using CleanHr.AuthApi.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using TanvirArjel.ArgumentChecker;
using CleanHr.AuthApi.Application.Services;
using Microsoft.Extensions.Logging;

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
            const string SymmetricKeyId = "MyAppSharedSecretKey"; // Must match the ID used in GetTokenAsync
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
                IssuerSigningKey = validationKey,
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    // Log the exception that caused the failure
                    logger.LogError(context.Exception, "JWT Token Validation failed. The reason is: {Message}", context.Exception.Message);

                    // The token string might be available for diagnostic purposes
                    var token = context.Properties.Items.TryGetValue(".Token.id", out var tokenValue)
                                                                                ? tokenValue : "Not available in context.";
                    logger.LogWarning("Failed token (partial/full): {Token}", token);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogInformation("Token validated successfully for user: {User}", context.Principal?.Identity?.Name);
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    logger.LogInformation("Authorization header: {AuthHeader}", authHeader);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogWarning("Authorization challenge. Error: {Error}, ErrorDescription: {ErrorDescription}",
                        context.Error, context.ErrorDescription);
                    return Task.CompletedTask;
                }
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
