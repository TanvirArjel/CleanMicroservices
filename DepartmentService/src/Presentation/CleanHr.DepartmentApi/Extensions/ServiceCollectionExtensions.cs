using CleanHr.DepartmentApi.Constants;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.DepartmentApi.Extensions;

internal static class ServiceCollectionExtensions
{
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
			.WithTracing(tracing => tracing
				.AddAspNetCoreInstrumentation(options =>
				{
					options.RecordException = true;
					options.Filter = (httpContext) =>
					{
						// Don't trace health check endpoints
						return !httpContext.Request.Path.StartsWithSegments("/healthz", StringComparison.OrdinalIgnoreCase);
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
				})
				.AddOtlpExporter(otlpOptions =>
				{
					otlpOptions.Endpoint = new Uri(configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint") ?? "http://localhost:4317");
				})
				.AddConsoleExporter());
	}
}
