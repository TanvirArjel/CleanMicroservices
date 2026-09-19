using System.Diagnostics;

namespace CleanHr.AuthApi.Common.Telemetry;

/// <summary>
/// Provides ActivitySource constants for application-level distributed tracing.
/// </summary>
public static class Tracing
{
    public const string SourceName = "AuthenicationService";

    public static readonly ActivitySource Source = new(SourceName, "1.0.0");
}
