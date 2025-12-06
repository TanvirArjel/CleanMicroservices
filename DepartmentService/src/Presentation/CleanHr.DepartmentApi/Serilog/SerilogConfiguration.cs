using System.Globalization;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;
using Serilog.Enrichers.Span;

namespace CleanHr.DepartmentApi.Serilog;

internal static class SerilogConfiguration
{
    public static void ConfigureSerilog()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .Enrich.With<CallerEnricher>()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("ServiceName", "DepartmentService")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.GrafanaLoki(
                "http://localhost:3100",
                labels:
                [
                    new LokiLabel { Key = "environment", Value = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" },
                    new LokiLabel { Key = "service_name", Value = "DepartmentService" }
                ],
                propertiesAsLabels:
                [
                    "TraceId",
                    "SpanId",
                    "ParentId",
                ],
                //textFormatter: new Serilog.Formatting.Compact.RenderedCompactJsonFormatter(),
                leavePropertiesIntact: true)
            .CreateLogger();
    }
}
