using TanvirArjel.ArgumentChecker;

namespace CleanHr.DepartmentApi.Infrastructure.Services.Configs;

public class SendGridConfig(string apiKey)
{
    public string ApiKey { get; set; } = apiKey.ThrowIfNullOrEmpty(nameof(apiKey));
}
