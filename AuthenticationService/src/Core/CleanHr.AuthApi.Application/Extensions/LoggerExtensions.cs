using Microsoft.Extensions.Logging;

namespace CleanHr.AuthApi.Application.Extensions;

public static class LoggerExtensions
{
#pragma warning disable CA2254 // Template should be a static expression
    public static void LogWithLevel<T>(
        this ILogger<T> logger,
        LogLevel logLevel,
        string message)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (logger.IsEnabled(logLevel))
        {
            logger.Log(logLevel, message);
        }
    }

    public static void LogWithLevel<T>(
        this ILogger<T> logger,
        LogLevel logLevel,
        string message,
        Dictionary<string, object> fields)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (logger.IsEnabled(logLevel))
        {
            logger.Log(logLevel, message, fields);
        }
    }

    public static void LogException<T>(
        this ILogger<T> logger,
        Exception exception,
        string message,
        Dictionary<string, object> fields)
    {
        ArgumentNullException.ThrowIfNull(logger);

        fields ??= [];
        fields.Add("ExceptionMessage", exception?.Message);
        fields.Add("ExceptionType", exception?.GetType().FullName ?? string.Empty);
        fields.Add("StackTrace", exception?.StackTrace ?? string.Empty);

        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(LogLevel.Error, exception, message, fields);
        }
    }
#pragma warning restore CA2254
}
