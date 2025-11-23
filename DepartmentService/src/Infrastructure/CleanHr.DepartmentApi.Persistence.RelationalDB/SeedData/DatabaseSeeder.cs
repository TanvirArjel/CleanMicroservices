using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CleanHr.DepartmentApi.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanHr.DepartmentApi.Persistence.RelationalDB.SeedData;

internal sealed class DatabaseSeeder(
    CleanHrDbContext dbContext,
    IDepartmentRepository departmentRepository,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync()
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            logger.LogInformation("Starting database seeding...");

            // Check if data already exists
            bool hasDepartments = await dbContext.Set<Department>().AnyAsync();

            if (hasDepartments)
            {
                logger.LogInformation("Database already contains departments. Skipping department seeding.");
            }
            else
            {
                logger.LogInformation("Seeding departments...");
                await SeedDepartmentsAsync();
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

    private async Task SeedDepartmentsAsync()
    {
        List<Department> departments = new();

        var departmentData = new[]
        {
            ("IT Department", "Information Technology department responsible for managing company's technical infrastructure and software development."),
            ("Human Resources", "Human Resources department handling employee relations, recruitment, and organizational development."),
            ("Finance", "Finance department managing company's financial operations, budgeting, and accounting activities."),
            ("Sales & Marketing", "Sales and Marketing department driving revenue growth and promoting company products and services."),
            ("Operations", "Operations department ensuring smooth day-to-day business operations and process optimization.")
        };

        foreach (var (name, description) in departmentData)
        {
            var result = await Department.CreateAsync(departmentRepository, name, description);

            if (result.IsSuccess)
            {
                departments.Add(result.Value);
            }
            else if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("Failed to create department '{DepartmentName}': {Errors}", name, result.Error);
            }
        }

        if (departments.Count > 0)
        {
            await dbContext.Set<Department>().AddRangeAsync(departments);
            await dbContext.SaveChangesAsync();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Seeded {Count} departments", departments.Count);
            }
        }
        else
        {
            logger.LogWarning("No departments were seeded due to validation errors");
        }
    }
}

