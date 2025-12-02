using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CleanHr.AuthApi.Domain.Models;

namespace CleanHr.AuthApi.Domain.Repositories;

public interface IApplicationUserRepository
{
    Task<bool> ExistsAsync(Expression<Func<ApplicationUser, bool>> predicate);

    Task<ApplicationUser> GetByIdAsync(Guid id);

    Task<ApplicationUser> GetByEmailAsync(string email);

    Task<ApplicationUser> GetByUserNameAsync(string userName);

    Task<ApplicationUser> GetByEmailOrUserNameAsync(string emailOrUserName);
}
