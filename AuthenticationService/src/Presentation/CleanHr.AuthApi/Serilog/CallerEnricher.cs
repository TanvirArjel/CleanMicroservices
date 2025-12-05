using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CleanHr.AuthApi.Serilog;

internal class CallerEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var skip = 3;
        while (true)
        {
            var stack = new StackFrame(skip);
            if (!stack.HasMethod())
            {
                return;
            }

            var method = stack.GetMethod();
            if (method == null)
            {
                return;
            }

            var type = method.DeclaringType;
            if (type == null)
            {
                return;
            }

            // Skip Serilog internal types
            var namespaceName = type.Namespace ?? string.Empty;
            if (namespaceName.StartsWith("Serilog", StringComparison.InvariantCultureIgnoreCase) ||
                namespaceName.StartsWith("Microsoft.Extensions.Logging", StringComparison.InvariantCultureIgnoreCase))
            {
                skip++;
                continue;
            }

            string methodName = method.Name;
            string className = type.Name;

            // Handle async/iterator methods (state machine classes)
            if (type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length > 0)
            {
                // Extract actual method name from state machine class name
                // Pattern: <MethodName>d__X or <>c__DisplayClassX
                var match = Regex.Match(className, @"<(\w+)>");
                if (match.Success)
                {
                    methodName = match.Groups[1].Value;
                }

                // Get the declaring type (parent class)
                if (type.DeclaringType != null)
                {
                    className = type.DeclaringType.Name;
                }
            }

            var caller = $"{className}.{methodName}";
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("MethodName", methodName));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ClassName", className));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Caller", caller));
            return;
        }
    }
}
