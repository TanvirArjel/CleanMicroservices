using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CleanHr.AuthApi.Application.Telemetry;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Models;
using CleanHr.AuthApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace CleanHr.AuthApi.Persistence.RelationalDB.Repositories;

[ScopedService]
internal sealed class ApplicationUserRepository : IApplicationUserRepository
{
    private readonly CleanHrDbContext _dbContext;
    private readonly ILogger<ApplicationUserRepository> _logger;

    public ApplicationUserRepository(CleanHrDbContext dbContext, ILogger<ApplicationUserRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ExistsAsync(Expression<Func<ApplicationUser, bool>> predicate)
    {
        return await _dbContext.Set<ApplicationUser>().AnyAsync(predicate);
    }

    public async Task<ApplicationUser> GetByIdAsync(Guid id)
    {
        return await _dbContext.Set<ApplicationUser>().FindAsync(id);
    }

    public async Task<ApplicationUser> GetByEmailAsync(string email)
    {
        return await _dbContext.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<ApplicationUser> GetByUserNameAsync(string userName)
    {
        return await _dbContext.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.UserName == userName);
    }

    public async Task<Result<ApplicationUser>> GetByEmailOrUserNameAsync(string emailOrUserName)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(
               "GetByEmailOrUserName",
               ActivityKind.Internal);
        activity.SetTag("query.identifier", emailOrUserName);

        try
        {
            string normalizedEmailOrUserName = emailOrUserName.ToUpperInvariant();
            ApplicationUser user = await _dbContext.Set<ApplicationUser>()
                .Where(u => u.NormalizedEmail == normalizedEmailOrUserName || u.NormalizedUserName == normalizedEmailOrUserName)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                _logger.LogInformation("User found with email/username: {EmailOrUserName}", emailOrUserName);
            }
            else
            {
                _logger.LogInformation("No user found with email/username: {EmailOrUserName}", emailOrUserName);
            }

            activity.SetStatus(ActivityStatusCode.Ok, "User retrieval successful");
            return Result<ApplicationUser>.Success(user);
        }
        catch (Exception ex)
        {
            activity.SetStatus(ActivityStatusCode.Error, "Error retrieving user");
            _logger.LogCritical(ex, "An error occurred while retrieving user with email/username: {EmailOrUserName}", emailOrUserName);
            return Result<ApplicationUser>.Failure("An error occurred while retrieving user.");
        }
    }
}
