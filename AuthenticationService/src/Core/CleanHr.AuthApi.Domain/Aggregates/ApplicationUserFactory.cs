using System;
using System.Threading.Tasks;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace CleanHr.AuthApi.Domain.Aggregates;

[ScopedService]
public sealed class ApplicationUserFactory
{
    private readonly IApplicationUserRepository _userRepository;

    public ApplicationUserFactory(IApplicationUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public Task<Result<ApplicationUser>> CreateAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        string userName = null)
    {
        return ApplicationUser.CreateAsync(_userRepository, firstName, lastName, email, password, userName);
    }
}
