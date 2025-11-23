using System;

namespace CleanHr.AuthApi.Domain.Aggregates;

public class UserOldPassword
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string PasswordHash { get; set; }

    public DateTime SetAtUtc { get; set; }

    public ApplicationUser User { get; set; }
}
