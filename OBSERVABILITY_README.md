# CleanMicroservices - Shared Observability Stack

This directory contains the shared observability infrastructure for all CleanHR microservices.

## Services

The observability stack monitors these services:

| Service | Port | Metrics Endpoint |
|---------|------|------------------|
| Auth API | 5000 | http://localhost:5000/metrics |
| Employee API | 5001 | http://localhost:5001/metrics |
| Department API | 5002 | http://localhost:5002/metrics |
| Blazor Client | 5003 | http://localhost:5003/metrics |

## Quick Start

### 1. Start Observability Stack

From the **root CleanMicroservices directory**:

```bash
docker-compose up -d
```

This starts:
- **Prometheus** - Metrics (http://localhost:9090)
- **Grafana** - Dashboards (http://localhost:3000)
- **Loki** - Logs (http://localhost:3100)
- **Tempo** - Traces (http://localhost:3200)
- **SQL Server** - Database (localhost:1440)

### 2. Start Services

```bash
# Auth Service
cd AuthenticationService/src/Presentation/CleanHr.AuthApi
dotnet run

# Employee Service (in new terminal)
cd EmployeeService/src/Presentation/CleanHr.EmployeeApi
dotnet run --urls "http://localhost:5001"

# Department Service (in new terminal)
cd DepartmentService/src/Presentation/CleanHr.DepartmentApi
dotnet run --urls "http://localhost:5002"

# Blazor Client (in new terminal)
cd ClientApps/src/CleanHr.Blazor
dotnet run --urls "http://localhost:5003"
```

### 3. Access Dashboards

- **Grafana**: http://localhost:3000 (no login required)
- **Prometheus**: http://localhost:9090
- **Services Health**: Check all targets at http://localhost:9090/targets

## Observability Stack Components

### Prometheus (Metrics)
- **Port**: 9090
- **Scrapes**: All 4 services every 10 seconds
- **Metrics**:
  - HTTP request duration and status codes (automatic via OpenTelemetry)
  - .NET runtime metrics (GC, memory, threads)
  - Custom business metrics

### Grafana (Visualization)
- **Port**: 3000
- **Datasources**: Prometheus, Loki, Tempo (all pre-configured)
- **Features**:
  - Cross-datasource correlation (metrics ↔ logs ↔ traces)
  - No login required (anonymous admin access)

### Loki (Logs)
- **Port**: 3100
- **Features**:
  - Structured JSON logs
  - TraceId/SpanId in every log
  - Jump from logs to traces

### Tempo (Traces)
- **Ports**: 3200 (UI), 4317 (OTLP gRPC), 4318 (OTLP HTTP)
- **Features**:
  - Distributed tracing across services
  - Parent-child span hierarchy
  - Jump from traces to logs/metrics

## Configuration Files

All services share these configurations:

- `prometheus-config.yaml` - Scrape targets for all 4 services
- `grafana-datasources.yaml` - Pre-configured datasources with correlations
- `loki-config.yaml` - Log aggregation settings
- `tempo-config.yaml` - Distributed tracing settings

## Service Configuration

Each service must be configured to send telemetry to the shared stack:

### OpenTelemetry Configuration (appsettings.json)

```json
{
  "OpenTelemetry": {
    "ServiceName": "CleanHr.<ServiceName>",
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

### Serilog Loki Configuration (appsettings.json)

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "GrafanaLoki",
        "Args": {
          "uri": "http://localhost:3100",
          "labels": [
            { "key": "app", "value": "cleanhr-<service-name>" },
            { "key": "environment", "value": "Development" }
          ]
        }
      }
    ]
  }
}
```

## Example Queries

### Prometheus

```promql
# Request rate across all services
sum(rate(http_server_request_duration_seconds_count[5m])) by (service)

# P95 latency by service
histogram_quantile(0.95, 
  sum(rate(http_server_request_duration_seconds_bucket[5m])) by (service, le))

# Error rate by service
sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"[45].."}[5m])) by (service)

# GC collections across all services
sum(rate(process_runtime_dotnet_gc_collections_count[5m])) by (service, generation)
```

### Loki

```logql
# All logs from all services
{application="cleanhr"}

# Auth service logs
{service="auth-api"}

# Errors across all services
{application="cleanhr"} |= "ERROR"

# Login-related logs
{application="cleanhr"} |= "login"
```

### Tempo

Search by:
- **Service**: auth-api, employee-api, department-api
- **TraceID**: From logs or metrics
- **Duration**: Find slow requests
- **Status**: Error or OK

## Correlation Example

1. **Prometheus**: Notice high error rate
   ```promql
   http_server_request_duration_seconds_count{service="auth-api", http_response_status_code="500"}
   ```

2. **Grafana → Loki**: Query logs for that timeframe
   ```logql
   {service="auth-api"} |= "ERROR" | json
   ```

3. **Extract TraceId** from log entry

4. **Grafana → Tempo**: Search by TraceId to see full distributed trace

## Stopping the Stack

```bash
# Stop all containers
docker-compose down

# Stop and remove all data
docker-compose down -v
```

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    CleanHR Services                         │
├─────────────────────────────────────────────────────────────┤
│  Auth API (5000)  │  Employee API (5001)  │  Department API │
│                   │                        │     (5002)      │
│  /metrics         │  /metrics              │  /metrics       │
│  Logs → Loki      │  Logs → Loki           │  Logs → Loki    │
│  Traces → Tempo   │  Traces → Tempo        │  Traces → Tempo │
└─────────┬─────────┴────────────┬───────────┴─────────────────┘
          │                      │
          │                      │
          ▼                      ▼
    ┌──────────┐          ┌──────────┐
    │Prometheus│◄─────────│  Tempo   │
    │  :9090   │          │  :3200   │
    └─────┬────┘          └────┬─────┘
          │                    │
          │    ┌───────────────┤
          │    │               │
          ▼    ▼               ▼
        ┌────────────────────────┐
        │      Grafana           │
        │       :3000            │
        │                        │
        │  ┌──────────────────┐  │
        │  │  Prometheus      │  │
        │  │  Loki            │  │
        │  │  Tempo           │  │
        │  └──────────────────┘  │
        └────────────────────────┘
                  ▲
                  │
            ┌─────┴─────┐
            │   Loki    │
            │   :3100   │
            └───────────┘
```

## Documentation

- **Complete Setup**: See `Observability/COMPLETE_STACK_QUICKSTART.md`
- **Metrics Reference**: See `AuthenticationService/PROMETHEUS_METRICS.md`
- **Migration Guide**: See `AuthenticationService/OTEL_MIGRATION_SUMMARY.md`

## Port Summary

| Service | Port | Purpose |
|---------|------|---------|
| Auth API | 5000 | Authentication service |
| Employee API | 5001 | Employee management |
| Department API | 5002 | Department management |
| Blazor Client | 5003 | Frontend application |
| Grafana | 3000 | Visualization |
| Loki | 3100 | Log aggregation |
| Tempo | 3200 | Trace query UI |
| Tempo OTLP gRPC | 4317 | Trace ingestion |
| Tempo OTLP HTTP | 4318 | Trace ingestion |
| Prometheus | 9090 | Metrics UI |
| SQL Server | 1440 | Database |

## Next Steps

1. Start the stack: `docker-compose up -d`
2. Start your services on their designated ports
3. Generate some traffic
4. Explore Grafana dashboards
5. Create custom dashboards combining data from all services
