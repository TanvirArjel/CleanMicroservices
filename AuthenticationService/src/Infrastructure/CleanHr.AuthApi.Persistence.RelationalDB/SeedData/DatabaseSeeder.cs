using System;
using System.Linq;
using System.Threading.Tasks;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Models;
using CleanHr.AuthApi.Domain.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanHr.AuthApi.Persistence.RelationalDB.SeedData;

internal sealed class DatabaseSeeder(
    CleanHrDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IApplicationUserRepository applicationUserRepository,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync()
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            logger.LogInformation("Starting database seeding...");

            // Check if data already exists
            bool hasRoles = await dbContext.Set<ApplicationRole>().AnyAsync();
            bool hasUsers = await dbContext.Set<ApplicationUser>().AnyAsync();

            if (hasRoles)
            {
                logger.LogInformation("Database already contains roles. Skipping role seeding.");
            }
            else
            {
                logger.LogInformation("Seeding roles...");
                await SeedRolesAsync();
            }

            if (hasUsers)
            {
                logger.LogInformation("Database already contains users. Skipping user seeding.");
            }
            else
            {
                logger.LogInformation("Seeding users...");
                await SeedUsersAsync();
            }

            await transaction.CommitAsync();
            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "An error occurred while seeding the database. Transaction rolled back.");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        string[] roleNames = ["Admin", "Manager", "Employee", "HR"];

        foreach (string roleName in roleNames)
        {
            ApplicationRole role = new()
            {
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            };

            IdentityResult result = await roleManager.CreateAsync(role);

            if (!result.Succeeded && logger.IsEnabled(LogLevel.Warning))
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogWarning("Failed to create role {RoleName}: {Errors}", roleName, errors);
            }
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeded {Count} roles", roleNames.Length);
        }
    }

    private async Task SeedUsersAsync()
    {
        // Create Admin user
        Result<ApplicationUser> adminUserResult = await ApplicationUser.CreateAsync(
            applicationUserRepository,
            "admin@cleanhr.com",
            "Admin@123",
            userName: null);

        if (!adminUserResult.IsSuccess)
        {
            logger.LogWarning("Failed to create admin user via factory method");
            return;
        }

        ApplicationUser adminUser = adminUserResult.Value;
        adminUser.EmailConfirmed = true;
        adminUser.PhoneNumber = "+1234567800";
        adminUser.PhoneNumberConfirmed = true;
        adminUser.SecurityStamp = Guid.NewGuid().ToString();

        IdentityResult adminResult = await userManager.CreateAsync(adminUser, "Admin@123");

        if (adminResult.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Created admin user: {Email}", adminUser.Email);
            }
        }
        else if (logger.IsEnabled(LogLevel.Warning))
        {
            string errors = string.Join(", ", adminResult.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to create admin user: {Errors}", errors);
        }

        // Create Manager user
        Result<ApplicationUser> managerUserResult = await ApplicationUser.CreateAsync(
            applicationUserRepository,
            "manager@cleanhr.com",
            "Manager@123",
            userName: null);

        if (!managerUserResult.IsSuccess)
        {
            logger.LogWarning("Failed to create manager user via factory method");
            return;
        }

        ApplicationUser managerUser = managerUserResult.Value;
        managerUser.EmailConfirmed = true;
        managerUser.PhoneNumber = "+1234567801";
        managerUser.PhoneNumberConfirmed = true;
        managerUser.SecurityStamp = Guid.NewGuid().ToString();

        IdentityResult managerResult = await userManager.CreateAsync(managerUser, "Manager@123");

        if (managerResult.Succeeded)
        {
            await userManager.AddToRoleAsync(managerUser, "Manager");

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Created manager user: {Email}", managerUser.Email);
            }
        }
        else if (logger.IsEnabled(LogLevel.Warning))
        {
            string errors = string.Join(", ", managerResult.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to create manager user: {Errors}", errors);
        }

        // Create HR user
        Result<ApplicationUser> hrUserResult = await ApplicationUser.CreateAsync(
            applicationUserRepository,
            "hr@cleanhr.com",
            "Hr@123",
            userName: null);

        if (!hrUserResult.IsSuccess)
        {
            logger.LogWarning("Failed to create HR user via factory method");
            return;
        }

        ApplicationUser hrUser = hrUserResult.Value;
        hrUser.EmailConfirmed = true;
        hrUser.PhoneNumber = "+1234567802";
        hrUser.PhoneNumberConfirmed = true;
        hrUser.SecurityStamp = Guid.NewGuid().ToString();

        IdentityResult hrResult = await userManager.CreateAsync(hrUser, "Hr@123");

        if (hrResult.Succeeded)
        {
            await userManager.AddToRoleAsync(hrUser, "HR");

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Created HR user: {Email}", hrUser.Email);
            }
        }
        else if (logger.IsEnabled(LogLevel.Warning))
        {
            string errors = string.Join(", ", hrResult.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to create HR user: {Errors}", errors);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeded 3 identity users");
        }
    }
}
