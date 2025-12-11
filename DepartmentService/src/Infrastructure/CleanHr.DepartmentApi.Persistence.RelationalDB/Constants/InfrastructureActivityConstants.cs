using System.Diagnostics;

namespace CleanHr.DepartmentApi.Persistence.RelationalDB.Constants;

public static class InfrastructureActivityConstants
{
    public const string SourceName = "CleanHr.DepartmentApi.Infrastructure";

    internal static readonly ActivitySource Source = new(SourceName, "1.0.0");
}
