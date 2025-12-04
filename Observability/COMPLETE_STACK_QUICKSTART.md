# Complete Observability Stack - Quick Start

This guide will get your complete observability stack running in under 2 minutes.

## Stack Overview

- **Prometheus** - Metrics collection and storage
- **Grafana** - Unified visualization (metrics, logs, traces)
- **Loki** - Log aggregation
- **Tempo** - Distributed tracing
- **SQL Server** - Application database

## Quick Start

### 1. Start the Observability Stack

```bash
cd src/Presentation/CleanHr.AuthApi
docker-compose up -d
```

This starts:
- ✅ Prometheus on `http://localhost:9090`
- ✅ Grafana on `http://localhost:3000`
- ✅ Loki on `http://localhost:3100`
- ✅ Tempo on `http://localhost:3200`
- ✅ SQL Server on `localhost:1440`

### 2. Start the Auth API

```bash
dotnet run
```

The API will be available at `http://localhost:5000`

### 3. Verify Everything is Working

**Check Metrics Endpoint:**
```bash
curl http://localhost:5000/metrics | head -20
```

**Check Prometheus Scraping:**
```bash
# Open Prometheus
open http://localhost:9090

# Go to Status → Targets
# You should see "cleanhr-auth-api" target as UP
```

**Open Grafana:**
```bash
open http://localhost:3000

# No login required - anonymous access enabled
# Go to Explore to start querying
```

## Access Points

| Service | URL | Purpose |
|---------|-----|---------|
| Auth API | http://localhost:5000 | Application |
| Metrics | http://localhost:5000/metrics | OpenTelemetry metrics |
| Swagger | http://localhost:5000/swagger | API documentation |
| Prometheus | http://localhost:9090 | Metrics UI |
| Grafana | http://localhost:3000 | Unified observability |
| Loki | http://localhost:3100 | Logs API |
| Tempo | http://localhost:3200 | Traces API |

## Example Workflow

### 1. Generate Some Traffic

```bash
# Login request (will generate metrics, logs, and traces)
curl -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "emailOrUserName": "test@example.com",
    "password": "Test@1234"
  }'
```

### 2. View Metrics in Prometheus

```bash
# Open Prometheus
open http://localhost:9090

# Try these queries:

# HTTP request rate
rate(http_server_request_duration_seconds_count[1m])

# Login attempts
auth_login_attempts

# GC collections
process_runtime_dotnet_gc_collections_count

# Request duration (95th percentile)
histogram_quantile(0.95, rate(http_server_request_duration_seconds_bucket[5m]))
```

### 3. View Logs in Grafana

```bash
# Open Grafana → Explore
open http://localhost:3000/explore

# Select "Loki" datasource
# Query: {service="auth-api"} |= "login"

# You'll see logs with TraceId for correlation
```

### 4. View Traces in Grafana

```bash
# In Grafana Explore, select "Tempo" datasource
# Search for recent traces

# Click on a trace to see:
# - LoginUser span
# - ValidatePassword child span
# - RecordLogin child span
# - Duration, tags, and status for each
```

### 5. Correlation Example

**Scenario:** Notice an error in Prometheus

1. **Prometheus**: See error spike
   ```promql
   http_server_request_duration_seconds_count{http_response_status_code="500"}
   ```

2. **Grafana (Loki)**: Query logs for that timeframe
   ```logql
   {service="auth-api"} |= "ERROR" | json | http_response_status_code="500"
   ```

3. **Extract TraceId** from log entry

4. **Grafana (Tempo)**: Search by TraceId
   - See full request flow
   - Identify which span failed
   - Check span tags for error details

## Grafana Datasources

All three datasources are pre-configured and connected:

### Prometheus (Metrics)
- **URL**: `http://prometheus:9090`
- **Features**:
  - HTTP request metrics (duration, status codes)
  - Runtime metrics (GC, memory, threads)
  - Business metrics (login attempts, tokens, validations)
  - Connected to Tempo for exemplars

### Loki (Logs)
- **URL**: `http://loki:3100`
- **Features**:
  - Structured JSON logs
  - TraceId/SpanId in every log
  - Derived fields to jump to traces
  - Connected to Tempo

### Tempo (Traces)
- **URL**: `http://tempo:3200`
- **Features**:
  - Distributed tracing
  - Parent-child span hierarchy
  - Connected to Loki for logs
  - Connected to Prometheus for metrics

## Pre-configured Integrations

The datasources are configured for seamless navigation:

```yaml
# Tempo → Loki (traces to logs)
tracesToLogs:
  datasourceUid: 'loki'

# Tempo → Prometheus (traces to metrics)
tracesToMetrics:
  datasourceUid: 'prometheus'

# Prometheus → Tempo (exemplars)
exemplarTraceIdDestinations:
  - datasourceUid: tempo

# Loki → Tempo (logs to traces via TraceId)
derivedFields:
  - datasourceUid: tempo
    matcherRegex: "traceID=(\\w+)"
```

## Stopping the Stack

```bash
# Stop all containers
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v
```

## Troubleshooting

### Prometheus Can't Scrape API

**Problem**: Target shows as DOWN in Prometheus

**Solution**: Ensure API is running on host:
```bash
# Check API is running
curl http://localhost:5000/metrics

# Check Docker can reach host
docker exec prometheus ping -c 1 host.docker.internal
```

### No Metrics Showing in Grafana

**Problem**: Grafana connected but no data

**Solution**: 
1. Check Prometheus has data: `open http://localhost:9090`
2. Verify targets are UP: Status → Targets
3. Generate some traffic to the API
4. Wait 10-15 seconds for scrape interval

### Logs Not Appearing in Loki

**Problem**: No logs in Grafana → Loki

**Solution**:
```bash
# Check Loki is receiving logs
curl http://localhost:3100/ready

# Check Serilog configuration in appsettings.Development.json
# Ensure Loki sink is enabled with correct URL
```

### Traces Not Showing in Tempo

**Problem**: No traces in Grafana → Tempo

**Solution**:
```bash
# Check Tempo is running
curl http://localhost:3200/ready

# Verify OTLP exporter configuration in appsettings
# Should point to http://localhost:4317
```

## Next Steps

1. **Create Dashboards**: Build Grafana dashboards combining metrics, logs, and traces
2. **Set Up Alerts**: Configure Prometheus alerting rules
3. **Add More Services**: Extend to other microservices
4. **Production Setup**: Configure persistence, retention, and security

## Example Grafana Dashboards

### HTTP Performance Dashboard

**Panels:**
- Request rate by endpoint
- P50, P95, P99 latencies
- Error rate (4xx, 5xx)
- Active requests gauge

### Runtime Health Dashboard

**Panels:**
- Heap size over time
- GC collections per second by generation
- Thread pool queue depth
- GC pause times (P95, P99)

### Business Metrics Dashboard

**Panels:**
- Login success rate
- Failed login reasons (pie chart)
- Active logins gauge
- Token generation rate
- Password validation success rate

## Configuration Files

All configuration files are in `src/Presentation/CleanHr.AuthApi/Observability/`:

- `prometheus-config.yaml` - Prometheus scrape configuration
- `grafana-datasources.yaml` - Grafana datasource definitions
- `loki-config.yaml` - Loki configuration
- `tempo-config.yaml` - Tempo configuration

## Architecture

```
┌─────────────────┐
│   Auth API      │
│  (localhost:5000)│
│                 │
│  /metrics       │───────┐
│  HTTP requests  │       │
│  Logs → Loki    │       │
│  Traces → Tempo │       │
└─────────────────┘       │
                          │
                          ▼
                ┌──────────────────┐
                │   Prometheus     │
                │  (localhost:9090) │
                │                  │
                │  Scrapes /metrics│
                │  every 10s       │
                └──────────────────┘
                          │
                          ▼
                ┌──────────────────┐
                │    Grafana       │
                │  (localhost:3000) │
                │                  │
                │  ┌─────────────┐ │
                │  │ Prometheus  │ │
                │  │ Loki        │ │
                │  │ Tempo       │ │
                │  └─────────────┘ │
                └──────────────────┘
```

Your complete observability stack is now running! 🎉
