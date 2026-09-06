using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CleanHr.AuthApi.Application.Metrics;

/// <summary>
/// Unified metrics telemetry service using generic instruments parameterized by operation tags.
/// </summary>
public sealed class ApplicationMetrics : IApplicationMetrics, IDisposable
{
    public const string MeterName = "CleanHr.AuthApi.Application";

    private readonly Meter _meter;

    // ===== Generic Instruments =====
    private readonly Counter<long> _operationCounter;
    private readonly UpDownCounter<int> _activeOperationsCounter;
    private readonly Histogram<double> _operationDurationHistogram;

    public ApplicationMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        _meter = meterFactory.Create(MeterName, "1.0.0");

        // Single generic Counter for tracking operation totals
        _operationCounter = _meter.CreateCounter<long>(
            "auth.operation.count",
            unit: "{operation}",
            description: "Total number of operations executed by operation type, status, and error type");

        // Single generic UpDownCounter for tracking active concurrent operations
        _activeOperationsCounter = _meter.CreateUpDownCounter<int>(
            "auth.operation.active",
            unit: "{operation}",
            description: "Number of currently active concurrent operations by operation type");

        // Generic Histogram for tracking operation execution duration
        _operationDurationHistogram = _meter.CreateHistogram<double>(
            "auth.operation.duration",
            unit: "ms",
            description: "Execution duration of operations in milliseconds");
    }

    public void RecordSuccessOperation(string operation)
    {
        RecordOperation(operation, "success", "none");
    }

    public void RecordFailureOperation(string operation, string errorType)
    {
        RecordOperation(operation, "failure", errorType);
    }

    public IDisposable TrackOperation(string operation)
    {
        var tags = new TagList { { "operation", operation } };
        _activeOperationsCounter.Add(1, tags);

        return new ActiveScope(_activeOperationsCounter, _operationDurationHistogram, tags);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    private void RecordOperation(string operation, string status, string errorType)
    {
        var tags = new TagList
        {
            { "operation", operation },
            { "status", status },
            { "error.type", errorType }
        };

        _operationCounter.Add(1, tags);
    }
}

/// <summary>
/// Internal scope handle that decrements active concurrent operation count 
/// and records execution duration upon disposal.
/// </summary>
internal sealed class ActiveScope : IDisposable
{
    private readonly UpDownCounter<int> _activeCounter;
    private readonly Histogram<double> _durationHistogram;
    private readonly TagList _tags;
    private readonly long _startTimestamp;
    private int _disposed;

    public ActiveScope(
        UpDownCounter<int> activeCounter,
        Histogram<double> durationHistogram,
        TagList tags)
    {
        _activeCounter = activeCounter;
        _durationHistogram = durationHistogram;
        _tags = tags;
        _startTimestamp = Stopwatch.GetTimestamp();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            // Calculate elapsed time in milliseconds accurately using GetElapsedTime
            double elapsedMilliseconds = Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds;

            _durationHistogram.Record(elapsedMilliseconds, _tags);
            _activeCounter.Add(-1, _tags);
        }
    }
}