namespace CleanHr.AuthApi.Features.User.Models;

public class EmailConfirmationModel
{
    public string Email { get; set; }

    public string Code { get; set; }
}
