using Microsoft.Extensions.Logging;

namespace CleanHr.AuthApi.Application.Extensions;

public static class LoggerExtensions
{
    public static void LogInformation(
        this ILogger logger,
        string message,
        Dictionary<string, object> logProperties)
    {
        using (logger?.BeginScope(logProperties))
        {
            logger.LogInformation("{Message}", message);
        }
    }

    public static void LogError(
        this ILogger logger,
        Exception exception,
        string message,
        Dictionary<string, object> logProperties)
    {
        using (logger?.BeginScope(logProperties))
        {
            logger.LogError(exception, "{Message}", message);
        }
    }
}
