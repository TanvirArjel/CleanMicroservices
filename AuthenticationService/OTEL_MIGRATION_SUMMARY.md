# OpenTelemetry Metrics Migration Summary

## What Changed

Successfully migrated from **prometheus-net** to **OpenTelemetry** for comprehensive metrics collection with Prometheus export.

## Key Benefits

### ✅ Automatic HTTP Metrics
- **Before**: Required manual instrumentation with `app.UseHttpMetrics()`
- **After**: Automatic collection via `OpenTelemetry.Instrumentation.AspNetCore`
- **Includes**: Request duration, status codes, active requests - per endpoint, no code changes needed

### ✅ Automatic Runtime Metrics  
- **Before**: Not available
- **After**: Automatic collection via `OpenTelemetry.Instrumentation.Runtime`
- **Includes**: CPU, memory, GC (gen0/1/2), heap size, thread pool, lock contention

### ✅ Simplified Business Metrics
- **Before**: Manual timing with `DateTime.UtcNow` and `Stopwatch`
- **After**: Focus only on counters and gauges, duration tracked automatically by spans
- **API**: Clean `System.Diagnostics.Metrics` instead of prometheus-net

### ✅ Unified Observability
- **Before**: Separate libraries for metrics (prometheus-net) and tracing (OpenTelemetry)
- **After**: Single OpenTelemetry framework for both metrics and traces
- **Correlation**: Automatic TraceId/SpanId in all metrics

---

## What Was Removed

### Removed Packages
- ❌ `prometheus-net` (from Application layer)
- ❌ `prometheus-net.AspNetCore` (replaced by OTel exporter)

### Removed Code
```csharp
// Manual HTTP metrics middleware
app.UseHttpMetrics();
app.MapMetrics();

// Manual duration tracking
var stopwatch = Stopwatch.StartNew();
try {
    // operation
} finally {
    ApplicationMetrics.SomeDuration.Observe(stopwatch.Elapsed.TotalSeconds);
}

// prometheus-net API
ApplicationMetrics.LoginAttempts.WithLabels("success", "none").Inc();
ApplicationMetrics.LoginDuration.WithLabels("success").Observe(duration);
```

### Removed Metrics
- `auth_login_duration_seconds` - Replaced by automatic HTTP metrics
- `auth_password_validation_duration_seconds` - Tracked via spans instead
- `auth_token_generation_duration_seconds` - Tracked via spans instead
- `auth_user_lookup_duration_seconds` - Tracked via spans instead

---

## What Was Added

### Added Packages
- ✅ `OpenTelemetry.Exporter.Prometheus.AspNetCore 1.14.0-beta.1`
- ✅ `OpenTelemetry.Instrumentation.Runtime 1.14.0`
- ✅ `OpenTelemetry.Metrics` (part of core OpenTelemetry)

### Added Configuration
```csharp
services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()      // HTTP metrics
        .AddRuntimeInstrumentation()         // .NET runtime metrics
        .AddHttpClientInstrumentation()      // Outbound HTTP metrics
        .AddMeter(ApplicationMetrics.MeterName) // Business metrics
        .AddPrometheusExporter())            // Export to Prometheus
```

### Added Endpoint
```csharp
// Startup.cs
app.MapPrometheusScrapingEndpoint(); // Exposes /metrics
```

### Updated Business Metrics
```csharp
// New System.Diagnostics.Metrics API
public static readonly Counter<long> LoginAttempts = 
    _meter.CreateCounter<long>("auth.login.attempts");

public static readonly UpDownCounter<int> ActiveLogins = 
    _meter.CreateUpDownCounter<int>("auth.login.active");

// Helper methods with TagList
public static void RecordLoginAttempt(string result, string errorType = "none")
{
    var tags = new TagList { { "result", result }, { "error_type", errorType } };
    LoginAttempts.Add(1, tags);
}
```

---

## Metric Name Mapping

### HTTP Metrics (NEW - Automatic)
| Metric | Description | Labels |
|--------|-------------|--------|
| `http_server_request_duration_seconds` | HTTP request duration | method, route, status_code |
| `http_server_active_requests` | Currently active requests | method, route |

### Runtime Metrics (NEW - Automatic)
| Metric | Description | Labels |
|--------|-------------|--------|
| `process_runtime_dotnet_gc_collections_count` | GC collections | generation |
| `process_runtime_dotnet_gc_heap_size_bytes` | Heap size | - |
| `process_runtime_dotnet_gc_duration_seconds` | GC pause duration | generation |
| `process_runtime_dotnet_thread_pool_thread_count` | Thread pool threads | - |
| `process_runtime_dotnet_thread_pool_queue_length` | Queued work items | - |
| `process_runtime_dotnet_monitor_lock_contention_count` | Lock contentions | - |

### Business Metrics (Updated)
| Old Name (prometheus-net) | New Name (OpenTelemetry) | Type | Labels |
|---------------------------|--------------------------|------|--------|
| `auth_login_attempts_total` | `auth_login_attempts` | Counter | result, error_type |
| `auth_active_logins` | `auth_login_active` | UpDownCounter | - |
| `auth_tokens_generated_total` | `auth_tokens_generated` | Counter | token_type |
| `auth_password_validations_total` | `auth_password_validations` | Counter | result |
| `auth_user_lookups_total` | `auth_user_lookups` | Counter | result |

---

## Code Changes

### ServiceCollectionExtensions.cs
```diff
+ using OpenTelemetry.Metrics;

  public static void AddOpenTelemetryTracing(...)
  {
      services.AddOpenTelemetry()
          .ConfigureResource(...)
+         .WithMetrics(metrics => metrics
+             .AddAspNetCoreInstrumentation()
+             .AddRuntimeInstrumentation()
+             .AddHttpClientInstrumentation()
+             .AddMeter(ApplicationMetrics.MeterName)
+             .AddPrometheusExporter())
          .WithTracing(...)
  }
```

### Startup.cs
```diff
- using Prometheus;
+ using OpenTelemetry.Metrics;

  public static void ConfigureApp(WebApplication app)
  {
      // ...
-     app.UseHttpMetrics();
-     app.MapMetrics();
+     app.MapPrometheusScrapingEndpoint();
  }
```

### ApplicationMetrics.cs
```diff
- using Prometheus;
+ using System.Diagnostics;
+ using System.Diagnostics.Metrics;

  public static class ApplicationMetrics
  {
+     public const string MeterName = "CleanHr.AuthApi.Application";
+     private static readonly Meter _meter = new(MeterName, "1.0.0");

-     public static readonly Counter LoginAttempts = Metrics.CreateCounter(
-         "auth_login_attempts_total", ...);
+     public static readonly Counter<long> LoginAttempts = 
+         _meter.CreateCounter<long>("auth.login.attempts", ...);

-     public static readonly Gauge ActiveLogins = Metrics.CreateGauge(...);
+     public static readonly UpDownCounter<int> ActiveLogins = 
+         _meter.CreateUpDownCounter<int>("auth.login.active", ...);

-     // Duration histograms removed - handled by HTTP instrumentation
  
+     // Helper methods
+     public static void RecordLoginAttempt(string result, string errorType)
+     {
+         var tags = new TagList { { "result", result }, { "error_type", errorType } };
+         LoginAttempts.Add(1, tags);
+     }
  }
```

### LoginUserCommand.cs
```diff
  public async Task<Result<AuthenticationResult>> Handle(...)
  {
      using var activity = ...;
-     ApplicationMetrics.ActiveLogins.Inc();
-     var startTime = DateTime.UtcNow;
+     ApplicationMetrics.ActiveLogins.Add(1);

      try
      {
          if (string.IsNullOrWhiteSpace(request.EmailOrUserName))
          {
-             ApplicationMetrics.LoginAttempts.WithLabels("validation_failed", "...").Inc();
+             ApplicationMetrics.RecordLoginAttempt("validation_failed", "...");
          }
          
          // ...
-         ApplicationMetrics.LoginAttempts.WithLabels("success", "none").Inc();
-         ApplicationMetrics.LoginDuration.WithLabels("success").Observe(duration);
+         ApplicationMetrics.RecordLoginAttempt("success", "none");
      }
      finally
      {
-         var duration = (DateTime.UtcNow - startTime).TotalSeconds;
-         ApplicationMetrics.LoginDuration.WithLabels("completed").Observe(duration);
-         ApplicationMetrics.ActiveLogins.Dec();
+         ApplicationMetrics.ActiveLogins.Add(-1);
      }
  }
```

---

## Prometheus Query Updates

### Old Queries (prometheus-net)
```promql
# Request duration - had to manually track
rate(auth_login_duration_seconds_sum[5m]) / 
rate(auth_login_duration_seconds_count[5m])

# Error rate - manual counters
rate(auth_login_attempts_total{result="error"}[5m])
```

### New Queries (OpenTelemetry)
```promql
# Request duration - automatic HTTP metrics
histogram_quantile(0.95, 
  rate(http_server_request_duration_seconds_bucket{http_route="/api/users/login"}[5m]))

# Error rate - automatic status code tracking
rate(http_server_request_duration_seconds_count{
  http_route="/api/users/login",
  http_response_status_code=~"[45].."
}[5m])

# Business metrics - similar API
rate(auth_login_attempts{result="success"}[5m])
```

---

## Testing

### Verify Metrics Endpoint
```bash
# Start application
cd src/Presentation/CleanHr.AuthApi
dotnet run

# Check metrics endpoint
curl http://localhost:5000/metrics | grep -E "(http_server|process_runtime|auth_)"

# Should see:
# - http_server_request_duration_seconds_bucket
# - process_runtime_dotnet_gc_collections_count
# - auth_login_attempts
# - auth_login_active
```

### Sample Output
```
# HELP http_server_request_duration_seconds Duration of HTTP server requests
# TYPE http_server_request_duration_seconds histogram
http_server_request_duration_seconds_bucket{http_method="POST",http_route="/api/users/login",http_response_status_code="200",le="0.005"} 42

# HELP process_runtime_dotnet_gc_collections_count Number of garbage collections
# TYPE process_runtime_dotnet_gc_collections_count counter
process_runtime_dotnet_gc_collections_count{generation="gen0"} 15
process_runtime_dotnet_gc_collections_count{generation="gen1"} 3
process_runtime_dotnet_gc_collections_count{generation="gen2"} 1

# HELP auth_login_attempts Total number of login attempts
# TYPE auth_login_attempts counter
auth_login_attempts{result="success",error_type="none"} 127
auth_login_attempts{result="failed",error_type="invalid_password"} 8

# HELP auth_login_active Number of currently active login operations
# TYPE auth_login_active gauge
auth_login_active 2
```

---

## Rollback Plan (If Needed)

If issues arise:

1. **Revert packages:**
   ```bash
   dotnet remove package OpenTelemetry.Exporter.Prometheus.AspNetCore
   dotnet remove package OpenTelemetry.Instrumentation.Runtime
   dotnet add package prometheus-net.AspNetCore
   ```

2. **Restore old ApplicationMetrics.cs** from git history
3. **Restore old ServiceCollectionExtensions.cs** from git history
4. **Restore old Startup.cs** from git history
5. **Restore old LoginUserCommand.cs** from git history

---

## Next Actions

1. ✅ **Build succeeds** - Verified
2. 🔄 **Test /metrics endpoint** - Run application and verify output
3. 📊 **Update Grafana dashboards** - Use new metric names in queries
4. 🔔 **Update alerting rules** - Adjust for new metric names
5. 📈 **Monitor runtime metrics** - Check GC, memory, thread pool behavior
6. 📝 **Document SLOs** - Define targets based on HTTP metrics

---

## Resources

- [OpenTelemetry Metrics Documentation](https://opentelemetry.io/docs/instrumentation/net/metrics/)
- [ASP.NET Core Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore)
- [Runtime Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Runtime)
- [Prometheus Exporter](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore)
