namespace CleanHr.EmployeeApi.Configs;

public record JwtConfig(string Issuer, string Key, int ExpiryInSeconds);
