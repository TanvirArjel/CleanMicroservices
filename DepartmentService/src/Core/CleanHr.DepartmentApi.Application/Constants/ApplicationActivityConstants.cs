using System.Diagnostics;

namespace CleanHr.DepartmentApi.Application.Constants;

public static class ApplicationActivityConstants
{
    // The source name specific to the Application layer
    public const string SourceName = "CleanHr.DepartmentApi.Application";

    internal static readonly ActivitySource Source = new(SourceName, "1.0.0");
}
