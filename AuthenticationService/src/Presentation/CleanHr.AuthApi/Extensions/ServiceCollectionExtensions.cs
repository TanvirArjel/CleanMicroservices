using System.Text;
using CleanHr.AuthApi.Application.Infrastructures;
using CleanHr.AuthApi.Application.Telemetry;
using CleanHr.AuthApi.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TanvirArjel.ArgumentChecker;
using CleanHr.AuthApi.Application.Services;

namespace CleanHr.AuthApi.Extensions;

internal static class ServiceCollectionExtensions
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

	public static void AddOpenTelemetryTracing(this IServiceCollection services, IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);

		services.AddOpenTelemetry()
			.ConfigureResource(resource => resource
				.AddService(
					serviceName: configuration.GetValue<string>("OpenTelemetry:ServiceName") ?? "CleanHrApi",
					serviceVersion: typeof(ServiceCollectionExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
            .WithMetrics(metrics =>
            {
                // ASP.NET Core HTTP metrics (request duration, status codes, etc.)
                metrics.AddAspNetCoreInstrumentation()
                // .NET Runtime metrics (CPU, memory, GC, thread pool, etc.)
                .AddRuntimeInstrumentation()
                // HTTP client metrics
                .AddHttpClientInstrumentation()
                // Custom business metrics
                .AddMeter(ApplicationMetrics.MeterName)
                // Export to Prometheus via /metrics endpoint
                .AddPrometheusExporter();
            })
            .WithTracing(tracing => tracing
                .AddSource(ApplicationDiagnostics.ActivitySourceName)
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = (httpContext) =>
                    {
                        // Don't trace health check endpoints
                        return !httpContext.Request.Path.StartsWithSegments("/healthz", StringComparison.OrdinalIgnoreCase);
                    };
                    options.EnrichWithHttpRequest = (activity, httpRequest) =>
					{
						activity.SetTag("http.request.content_type", httpRequest.ContentType);
						activity.SetTag("http.request.content_length", httpRequest.ContentLength);

                        // Resolve API version in route template for metrics and traces
                        var apiVersion = httpRequest.HttpContext.GetRequestedApiVersion();
                        if (apiVersion != null)
                        {
                            var route = activity.GetTagItem("http.route")?.ToString();
                            if (!string.IsNullOrEmpty(route) && route.Contains("{version:apiVersion}", StringComparison.OrdinalIgnoreCase))
                            {
                                var resolvedRoute = route.Replace("{version:apiVersion}", apiVersion.ToString(), StringComparison.OrdinalIgnoreCase);
                                activity.SetTag("http.route", resolvedRoute);
                            }
                        }
                    };
					options.EnrichWithHttpResponse = (activity, httpResponse) =>
					{
						activity.SetTag("http.response.content_type", httpResponse.ContentType);
						activity.SetTag("http.response.content_length", httpResponse.ContentLength);
					};
				})
				.AddHttpClientInstrumentation(options =>
				{
					options.RecordException = true;
				})
				.AddSqlClientInstrumentation(options =>
				{
					options.SetDbStatementForText = true;
					options.RecordException = true;
					options.EnableConnectionLevelAttributes = true;
				})
				.AddOtlpExporter(otlpOptions =>
				{
					otlpOptions.Endpoint = new Uri(configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint") ?? "http://localhost:4317");
				})
				.AddConsoleExporter());
	}
}
