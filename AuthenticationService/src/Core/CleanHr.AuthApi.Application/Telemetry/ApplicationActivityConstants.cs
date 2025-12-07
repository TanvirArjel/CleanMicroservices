using System.Diagnostics;

namespace CleanHr.AuthApi.Application.Telemetry;

/// <summary>
/// Provides ActivitySource constants for application-level distributed tracing.
/// </summary>
public static class ApplicationActivityConstants
{
    public const string SourceName = "CleanHr.AuthApi.Application";

    internal static readonly ActivitySource Source = new(SourceName, "1.0.0");
}
