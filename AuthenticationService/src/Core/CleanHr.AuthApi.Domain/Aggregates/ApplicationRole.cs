using System;
using Microsoft.AspNetCore.Identity;

namespace CleanHr.AuthApi.Domain.Aggregates;

public class ApplicationRole : IdentityRole<Guid>
{
}
