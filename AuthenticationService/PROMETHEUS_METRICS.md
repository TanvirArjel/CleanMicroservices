# OpenTelemetry Metrics with Prometheus Exporter

## Overview

The Authentication Service uses **OpenTelemetry** for comprehensive observability with metrics exported to Prometheus. This provides automatic instrumentation for HTTP requests, runtime metrics, and custom business metrics - all accessible via the `/metrics` endpoint.

## Metrics Architecture

### Three Types of Metrics

1. **HTTP Metrics** (Automatic - OpenTelemetry ASP.NET Core)
   - Request duration, count, and status codes
   - Automatic per-endpoint metrics
   - No manual instrumentation required

2. **Runtime Metrics** (Automatic - OpenTelemetry Runtime)
   - CPU usage, memory consumption
   - Garbage collection statistics
   - Thread pool metrics
   - No manual instrumentation required

3. **Business Metrics** (Custom - Application Layer)
   - Login attempts and outcomes
   - Token generation counts
   - Password validation results
   - Requires manual instrumentation

---

## Automatic HTTP Metrics (OpenTelemetry ASP.NET Core)

These metrics are automatically collected for all HTTP endpoints:

### `http_server_request_duration_seconds` (Histogram)
Duration of HTTP requests in seconds.

**Labels:**
- `http_method`: GET, POST, PUT, DELETE, etc.
- `http_route`: Route template (e.g., `/api/users/login`)
- `http_response_status_code`: 200, 400, 401, 500, etc.
- `network_protocol_version`: HTTP version (1.1, 2.0)

**Example Queries:**
```promql
# 95th percentile response time by endpoint
histogram_quantile(0.95, 
  rate(http_server_request_duration_seconds_bucket[5m])) by (http_route)

# Request rate by status code
rate(http_server_request_duration_seconds_count{http_response_status_code=~"2.."}[5m])

# Error rate (4xx and 5xx)
rate(http_server_request_duration_seconds_count{http_response_status_code=~"[45].."}[5m])
```

### `http_server_active_requests` (UpDownCounter)
Number of currently active HTTP requests.

**Example Queries:**
```promql
# Current active requests
http_server_active_requests

# Max concurrent requests over time
max_over_time(http_server_active_requests[1h])
```

---

## Automatic Runtime Metrics (OpenTelemetry .NET Runtime)

### Process Metrics

#### `process_runtime_dotnet_gc_collections_count` (Counter)
Number of garbage collections by generation.

**Labels:**
- `generation`: gen0, gen1, gen2

```promql
# GC collections per second by generation
rate(process_runtime_dotnet_gc_collections_count[5m]) by (generation)
```

#### `process_runtime_dotnet_gc_heap_size_bytes` (UpDownCounter)
Size of the managed heap.

```promql
# Current heap size in MB
process_runtime_dotnet_gc_heap_size_bytes / 1024 / 1024
```

#### `process_runtime_dotnet_gc_duration_seconds` (Histogram)
Duration of garbage collection pauses.

```promql
# 99th percentile GC pause time
histogram_quantile(0.99, rate(process_runtime_dotnet_gc_duration_seconds_bucket[5m]))
```

### Thread Pool Metrics

#### `process_runtime_dotnet_thread_pool_thread_count` (UpDownCounter)
Number of thread pool threads.

#### `process_runtime_dotnet_thread_pool_queue_length` (UpDownCounter)
Number of work items queued to the thread pool.

```promql
# Thread pool saturation
process_runtime_dotnet_thread_pool_queue_length
```

### Memory Metrics

#### `process_runtime_dotnet_monitor_lock_contention_count` (Counter)
Monitor lock contention count.

```promql
# Lock contention rate
rate(process_runtime_dotnet_monitor_lock_contention_count[5m])
```

---

## Custom Business Metrics

Located in `ApplicationMetrics.cs`, these track application-specific operations.

### Login Metrics

#### `auth_login_attempts` (Counter)
Total number of login attempts categorized by result and error type.

**Labels:**
- `result`: success, failed, validation_failed, error
- `error_type`: none, missing_email_or_username, missing_password, user_not_found, invalid_password, token_generation_failed, or exception name

**Example Queries:**
```promql
# Login success rate
rate(auth_login_attempts{result="success"}[5m]) /
rate(auth_login_attempts[5m]) * 100

# Failed login rate by error type
rate(auth_login_attempts{result="failed"}[5m]) by (error_type)

# Validation errors
rate(auth_login_attempts{result="validation_failed"}[5m]) by (error_type)
```

#### `auth_login_active` (UpDownCounter)
Number of currently active login operations.

**Example Queries:**
```promql
# Current active logins
auth_login_active

# Peak concurrent logins
max_over_time(auth_login_active[1h])
```

---

### Token Metrics

#### `auth_tokens_generated` (Counter)
Total number of JWT tokens generated.

**Labels:**
- `token_type`: access, refresh

**Example Queries:**
```promql
# Token generation rate by type
rate(auth_tokens_generated[5m]) by (token_type)
```

---

### Password Validation Metrics

#### `auth_password_validations` (Counter)
Total number of password validation attempts.

**Labels:**
- `result`: success, failed

**Example Queries:**
```promql
# Password validation failure rate
rate(auth_password_validations{result="failed"}[5m]) /
rate(auth_password_validations[5m]) * 100
```

---

### User Lookup Metrics

#### `auth_user_lookups` (Counter)
Total number of user lookup operations.

**Labels:**
- `result`: found, not_found

**Example Queries:**
```promql
# User not found rate (potential attacks or typos)
rate(auth_user_lookups{result="not_found"}[5m])
```

---

## Accessing Metrics

### Local Development with Docker Compose

**Quick Start:**
```bash
# 1. Start the observability stack (Prometheus, Grafana, Loki, Tempo)
cd src/Presentation/CleanHr.AuthApi
docker-compose up -d

# 2. Start the Auth API application
dotnet run

# 3. Access the UIs
# - API Metrics: http://localhost:5000/metrics
# - Prometheus:  http://localhost:9090
# - Grafana:     http://localhost:3000
```

**View Raw Metrics:**
```bash
# View OpenTelemetry metrics in Prometheus format
curl http://localhost:5000/metrics | grep -E "(http_server|process_runtime|auth_)"
```

**Query in Prometheus:**
```bash
# Open Prometheus UI
open http://localhost:9090

# Example queries:
# - http_server_request_duration_seconds_bucket
# - process_runtime_dotnet_gc_collections_count
# - auth_login_attempts
```

**Visualize in Grafana:**
```bash
# Open Grafana (no login required)
open http://localhost:3000

# Navigate to Explore → Select "Prometheus" datasource
# All datasources are pre-configured (Prometheus, Loki, Tempo)
```

### Prometheus Configuration

The Prometheus configuration is in `Observability/prometheus-config.yaml` and automatically scrapes the Auth API:

```yaml
scrape_configs:
  - job_name: 'cleanhr-auth-api'
    scrape_interval: 10s
    metrics_path: '/metrics'
    static_configs:
      - targets: ['host.docker.internal:5000']  # Scrapes API running on host
        labels:
          service: 'auth-api'
          application: 'cleanhr'
```

**Note:** `host.docker.internal` allows Prometheus (running in Docker) to scrape the API running on your host machine.

### Grafana Datasources

Grafana is pre-configured with three datasources in `Observability/grafana-datasources.yaml`:

1. **Prometheus** → Metrics (HTTP, Runtime, Business)
2. **Loki** → Logs (with TraceId correlation)
3. **Tempo** → Distributed Traces

All datasources are automatically connected for seamless navigation between signals.

---

## Implementation Details

### Packages Used

**OpenTelemetry Core:**
- `OpenTelemetry.Extensions.Hosting 1.10.0`
- `OpenTelemetry.Instrumentation.AspNetCore 1.10.1`
- `OpenTelemetry.Instrumentation.Http 1.14.0`
- `OpenTelemetry.Instrumentation.SqlClient 1.10.0-beta.1`
- `OpenTelemetry.Instrumentation.Runtime 1.14.0`

**Exporters:**
- `OpenTelemetry.Exporter.Prometheus.AspNetCore 1.14.0-beta.1`
- `OpenTelemetry.Exporter.OpenTelemetryProtocol 1.10.0` (OTLP for Jaeger/Tempo)
- `OpenTelemetry.Exporter.Console 1.10.0` (debugging)

### Code Locations

**Configuration:**
- `src/Presentation/CleanHr.AuthApi/Extensions/ServiceCollectionExtensions.cs`
  - `AddOpenTelemetryTracing()` method configures metrics and tracing

**Business Metrics:**
- `src/Core/CleanHr.AuthApi.Application/Telemetry/ApplicationMetrics.cs`
  - Custom Meter with business-specific counters

**Usage:**
- `src/Core/CleanHr.AuthApi.Application/Commands/LoginUserCommand.cs`
  - Uses `ApplicationMetrics.RecordLoginAttempt()`, `RecordPasswordValidation()`, etc.

**Endpoint:**
- `src/Presentation/CleanHr.AuthApi/Startup.cs`
  - `app.MapPrometheusScrapingEndpoint()` exposes `/metrics`

### Configuration Example

```csharp
services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "CleanHr.AuthApi", serviceVersion: "1.0.0"))
    .WithMetrics(metrics => metrics
        // Automatic HTTP metrics
        .AddAspNetCoreInstrumentation()
        // Automatic runtime metrics (CPU, memory, GC, threads)
        .AddRuntimeInstrumentation()
        // HTTP client metrics
        .AddHttpClientInstrumentation()
        // Custom business metrics
        .AddMeter(ApplicationMetrics.MeterName)
        // Export to Prometheus
        .AddPrometheusExporter())
    .WithTracing(tracing => tracing
        // ... tracing configuration
    );
```

---

## Grafana Dashboard Examples

### HTTP Performance Panel
```promql
# Request duration by endpoint (95th percentile)
histogram_quantile(0.95,
  sum(rate(http_server_request_duration_seconds_bucket[5m])) by (http_route, le))
```

### Error Rate Panel
```promql
# Error rate (4xx + 5xx) percentage
sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"[45].."}[5m])) /
sum(rate(http_server_request_duration_seconds_count[5m])) * 100
```

### Runtime Health Panel
```promql
# Heap size in MB
process_runtime_dotnet_gc_heap_size_bytes / 1024 / 1024

# GC frequency
rate(process_runtime_dotnet_gc_collections_count[5m])

# Thread pool queue depth
process_runtime_dotnet_thread_pool_queue_length
```

### Business Metrics Panel
```promql
# Login success rate
rate(auth_login_attempts{result="success"}[5m]) /
rate(auth_login_attempts[5m]) * 100

# Active logins
auth_login_active
```

---

## Correlation with Logs and Traces

All operations include **TraceId** and **SpanId** for correlation:

1. **Metrics** → View error rate spike at timestamp
2. **Logs (Loki)** → Query logs with TraceId filter in that time range
3. **Traces (Jaeger/Tempo)** → Search by TraceId to see full request flow

### Example Correlation Workflow
```bash
# 1. Notice high error rate in Prometheus
http_server_request_duration_seconds_count{http_response_status_code="500"}

# 2. Query Loki for errors in same timeframe
{service="auth-api"} |= "ERROR" | json | http_response_status_code="500"

# 3. Extract TraceId from log entry
# 4. Search Jaeger for trace: traceID=abc123...

# 5. Analyze span timing and tags for root cause
```

---

## Differences from prometheus-net

| Feature | prometheus-net (Old) | OpenTelemetry (New) |
|---------|---------------------|---------------------|
| HTTP Metrics | Manual `UseHttpMetrics()` | Automatic via instrumentation |
| Runtime Metrics | Not available | Automatic (CPU, memory, GC) |
| Metric Names | Custom (`auth_login_attempts_total`) | Semantic conventions |
| Duration Tracking | Manual `Stopwatch` | Automatic span timing |
| API | `Counter.Inc()`, `.WithLabels()` | `Counter.Add()`, `TagList` |
| Exporter | Native Prometheus | OpenTelemetry Protocol |
| Integration | Standalone | Part of unified observability |

---

## Migration Notes

**Removed:**
- `prometheus-net` dependency (Application layer)
- Manual `LoginDuration`, `PasswordValidationDuration` histograms
- Manual `DateTime.UtcNow` timing
- `app.UseHttpMetrics()` middleware

**Added:**
- OpenTelemetry metrics instrumentation
- System.Diagnostics.Metrics API for business metrics
- Automatic HTTP and runtime metrics
- Unified `/metrics` endpoint via OpenTelemetry

**HTTP request duration is now automatic** - no need to manually track timing in application code. Focus on business metrics only.

---

## Next Steps

1. **Update Grafana Dashboards**: Adjust queries for new metric names
2. **Configure Alerts**: Set thresholds for HTTP errors, GC pauses, login failures
3. **Add More Business Metrics**: Token refresh, user registration, password reset
4. **Enable Remote Write**: Configure Prometheus to send metrics to long-term storage
5. **Set Up SLOs**: Define Service Level Objectives based on request duration and error rate

## Overview

Prometheus metrics have been successfully integrated into the Authentication Service. The metrics are exposed at the `/metrics` endpoint and track login operations with detailed labels for filtering and analysis.

## Available Metrics

### Login Metrics

#### `auth_login_attempts_total` (Counter)
Tracks the total number of login attempts with outcome classification.

**Labels:**
- `result`: The outcome of the login attempt
  - `success`: Login successful
  - `failed`: Login failed (invalid password, user not found)
  - `validation_failed`: Input validation failed
  - `error`: Unexpected error occurred
- `error_type`: Type of error encountered
  - `missing_email_or_username`: Email/username not provided
  - `missing_password`: Password not provided
  - `user_not_found`: User doesn't exist
  - `invalid_password`: Incorrect password
  - `<ExceptionType>`: Name of exception for errors

**Example Queries:**
```promql
# Total login attempts
sum(auth_login_attempts_total)

# Successful login rate
rate(auth_login_attempts_total{result="success"}[5m])

# Failed login rate by error type
rate(auth_login_attempts_total{result="failed"}[5m]) by (error_type)
```

---

#### `auth_login_duration_seconds` (Histogram)
Measures the duration of login operations in seconds.

**Labels:**
- `result`: Outcome of the operation
  - `completed`: Normal completion
  - `error`: Failed with error

**Buckets:** Exponential from 0.001s (1ms) to ~1s
- 0.001, 0.002, 0.004, 0.008, 0.016, 0.032, 0.064, 0.128, 0.256, 0.512

**Example Queries:**
```promql
# 95th percentile login duration
histogram_quantile(0.95, rate(auth_login_duration_seconds_bucket[5m]))

# Average login duration
rate(auth_login_duration_seconds_sum[5m]) / rate(auth_login_duration_seconds_count[5m])

# Requests slower than 100ms
sum(rate(auth_login_duration_seconds_bucket{le="0.1"}[5m]))
```

---

#### `auth_active_logins` (Gauge)
Current number of active login operations being processed.

**Example Queries:**
```promql
# Current active logins
auth_active_logins

# Maximum concurrent logins over time
max_over_time(auth_active_logins[1h])
```

---

### Password Validation Metrics

#### `auth_password_validations_total` (Counter)
Total number of password validation attempts.

**Labels:**
- `result`: Validation outcome
  - `success`: Password matches
  - `failed`: Password doesn't match

**Example Queries:**
```promql
# Password validation failure rate
rate(auth_password_validations_total{result="failed"}[5m])

# Success rate percentage
100 * rate(auth_password_validations_total{result="success"}[5m]) / rate(auth_password_validations_total[5m])
```

---

#### `auth_password_validation_duration_seconds` (Histogram)
Duration of password validation operations.

**Buckets:** Exponential from 0.001s (1ms) to ~1s

**Example Queries:**
```promql
# 99th percentile password validation time
histogram_quantile(0.99, rate(auth_password_validation_duration_seconds_bucket[5m]))
```

---

### Token Generation Metrics

#### `auth_tokens_generated_total` (Counter)
Total number of JWT tokens generated.

**Labels:**
- `token_type`: Type of token
  - `access`: Access token
  - `refresh`: Refresh token

---

#### `auth_token_generation_duration_seconds` (Histogram)
Duration of token generation operations.

---

### User Lookup Metrics

#### `auth_user_lookups_total` (Counter)
Total number of user lookup operations.

**Labels:**
- `result`: Lookup outcome
  - `found`: User found
  - `not_found`: User not found

---

#### `auth_user_lookup_duration_seconds` (Histogram)
Duration of user lookup operations.

---

## Accessing Metrics

### Local Development
```bash
# Start the application
dotnet run

# View metrics
curl http://localhost:5000/metrics
```

### Prometheus Configuration

Add this scrape config to your `prometheus.yml`:

```yaml
scrape_configs:
  - job_name: 'cleanhr-auth-api'
    scrape_interval: 15s
    static_configs:
      - targets: ['localhost:5000']
        labels:
          service: 'auth-api'
          environment: 'development'
```

## Grafana Dashboard Examples

### Login Success Rate Panel
```promql
sum(rate(auth_login_attempts_total{result="success"}[5m])) /
sum(rate(auth_login_attempts_total[5m])) * 100
```

### Login Duration Heatmap
```promql
sum(rate(auth_login_duration_seconds_bucket[5m])) by (le)
```

### Active Logins Gauge
```promql
auth_active_logins
```

### Error Rate by Type
```promql
sum(rate(auth_login_attempts_total{result=~"failed|error"}[5m])) by (error_type)
```

## Correlation with Logs and Traces

All login operations include:
- **TraceId**: Distributed trace identifier
- **SpanId**: Current span identifier
- **ParentId**: Parent span identifier

These IDs are available in:
1. **Logs (Loki)**: Automatically included in all log entries
2. **Traces (Jaeger/Tempo)**: Standard OpenTelemetry trace context
3. **Metrics**: Can be correlated via timestamps

### Example Correlation Workflow
1. Notice high error rate in Prometheus: `auth_login_attempts_total{result="error"}`
2. Query Loki for error logs in same time range with filters
3. Extract TraceId from log entry
4. Search Jaeger/Tempo for the trace to see full request flow
5. Analyze detailed span timing and tags

## Implementation Details

### Code Location
- **Metrics Definition**: `src/Core/CleanHr.AuthApi.Application/Telemetry/ApplicationMetrics.cs`
- **Usage**: `src/Core/CleanHr.AuthApi.Application/Commands/LoginUserCommand.cs`
- **Middleware**: `src/Presentation/CleanHr.AuthApi/Startup.cs`

### Middleware Configuration
```csharp
// Automatic HTTP metrics (request count, duration, etc.)
app.UseHttpMetrics();

// Expose /metrics endpoint
app.MapMetrics();
```

### Manual Instrumentation Pattern
```csharp
// Counter
ApplicationMetrics.LoginAttempts
    .WithLabels("success", "none")
    .Inc();

// Histogram with manual timing
var startTime = DateTime.UtcNow;
try 
{
    // Operation
}
finally 
{
    var duration = (DateTime.UtcNow - startTime).TotalSeconds;
    ApplicationMetrics.LoginDuration
        .WithLabels("completed")
        .Observe(duration);
}

// Gauge
ApplicationMetrics.ActiveLogins.Inc();  // Start
// ... operation
ApplicationMetrics.ActiveLogins.Dec();  // End
```

## Next Steps

1. **Configure Prometheus**: Set up Prometheus to scrape the `/metrics` endpoint
2. **Create Grafana Dashboards**: Build visualizations for key metrics
3. **Set Up Alerts**: Define alerting rules for critical thresholds
4. **Add More Metrics**: Extend to other endpoints (token refresh, user registration, etc.)

## Additional Resources

- [prometheus-net Documentation](https://github.com/prometheus-net/prometheus-net)
- [Prometheus Query Language](https://prometheus.io/docs/prometheus/latest/querying/basics/)
- [Grafana Dashboard Best Practices](https://grafana.com/docs/grafana/latest/best-practices/)
