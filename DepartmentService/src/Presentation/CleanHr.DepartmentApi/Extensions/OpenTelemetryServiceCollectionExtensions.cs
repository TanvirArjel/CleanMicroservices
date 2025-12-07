using CleanHr.DepartmentApi.Application.Constants;
using CleanHr.DepartmentApi.Persistence.RelationalDB.Constants;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CleanHr.DepartmentApi.Extensions;

internal static class OpenTelemetryServiceCollectionExtensions
{
    public static void AddOpenTelemetryTracing(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                string serviceName = configuration.GetValue<string>("OpenTelemetry:ServiceName") ?? "DepartmentService";
                string serviceVersion = configuration.GetValue<string>("OpenTelemetry:ServiceVersion") ?? "1.0.0";
                resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion);
            })
            .WithMetrics(metrics =>
            {
                // ASP.NET Core HTTP metrics (request duration, status codes, etc.)
                metrics.AddAspNetCoreInstrumentation()
                        // .NET Runtime metrics (CPU, memory, GC, thread pool, etc.)
                        .AddRuntimeInstrumentation()
                        // HTTP client metrics
                        .AddHttpClientInstrumentation()
                        // Export to Prometheus via /metrics endpoint
                        .AddPrometheusExporter();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(ApplicationActivityConstants.SourceName);
                tracing.AddSource(InfrastructureActivityConstants.SourceName);

                tracing.AddAspNetCoreInstrumentation(options =>
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
                .AddConsoleExporter();
            });
    }
}
