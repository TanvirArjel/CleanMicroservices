using System.Diagnostics;

namespace CleanHr.AuthApi.Persistence.RelationalDB.Constants;

public sealed class InfrastructureActivityConstants
{
    // The source name specific to the Infrastructure layer
    public const string SourceName = "CleanHr.AuthApi.Infrastructure";

    public static readonly ActivitySource Source = new(SourceName, "1.0.0");
}
