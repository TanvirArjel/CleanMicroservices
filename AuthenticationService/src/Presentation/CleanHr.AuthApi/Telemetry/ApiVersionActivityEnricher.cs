using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CleanHr.AuthApi.Telemetry;

/// <summary>
/// Enriches OpenTelemetry activities with resolved API version in http.route tag
/// </summary>
public class ApiVersionActivityEnricher : IDisposable
{
    private readonly ActivityListener _activityListener;

    public ApiVersionActivityEnricher()
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = OnActivityStopped
        };

        ActivitySource.AddActivityListener(_activityListener);
    }

    private static void OnActivityStopped(Activity activity)
    {
        // Only process HTTP server activities
        if (activity.OperationName != "Microsoft.AspNetCore.Hosting.HttpRequestIn")
        {
            return;
        }

        // Get the http.route tag
        var route = activity.GetTagItem("http.route")?.ToString();
        if (string.IsNullOrEmpty(route) || !route.Contains("{version:apiVersion}", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Try to get the API version from the activity tags or baggage
        var apiVersion = activity.GetTagItem("aspnetcore.api_version")?.ToString();

        // If not found in tags, try to extract from the actual path
        var path = activity.GetTagItem("url.path")?.ToString();
        if (!string.IsNullOrEmpty(path))
        {
            // Extract version from path like /api/v1/user/login
            var match = System.Text.RegularExpressions.Regex.Match(path, @"/api/v(\d+(?:\.\d+)?)/");
            if (match.Success)
            {
                apiVersion = match.Groups[1].Value;
            }
        }

        // Replace the version placeholder with actual version
        if (!string.IsNullOrEmpty(apiVersion))
        {
            var resolvedRoute = route.Replace("{version:apiVersion}", apiVersion, StringComparison.OrdinalIgnoreCase);
            activity.SetTag("http.route", resolvedRoute);
        }
    }

    public void Dispose()
    {
        _activityListener?.Dispose();
        GC.SuppressFinalize(this);
    }
}
