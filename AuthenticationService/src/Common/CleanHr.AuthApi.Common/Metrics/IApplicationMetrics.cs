namespace CleanHr.AuthApi.Common.Metrics;

public interface IApplicationMetrics
{
    // Record total attempts for any operation
    void RecordSuccessOperation(string operation);
    void RecordFailureOperation(string operation, string errorType);

    // Begin an active scope tracking concurrent execution for any operation
    IDisposable TrackOperation(string operation);
}