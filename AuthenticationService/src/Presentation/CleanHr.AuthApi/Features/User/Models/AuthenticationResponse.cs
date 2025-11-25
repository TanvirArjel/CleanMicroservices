namespace CleanHr.AuthApi.Features.User.Models;

public class AuthenticationResponse
{
    public string AccessToken { get; set; }

    public string RefreshToken { get; set; }

    public int ExpiresIn { get; set; }

    public string TokenType { get; set; } = "Bearer";
}
