using CleanHr.DepartmentApi.Constants;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CleanHr.DepartmentApi.Extensions;

internal static class HealthCheckServiceCollectionExtensions
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
}
