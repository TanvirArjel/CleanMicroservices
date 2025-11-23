using System;
using CleanHr.DepartmentApi.Domain.Aggregates;
using CleanHr.DepartmentApi.Persistence.RelationalDB.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.DepartmentApi.Persistence.RelationalDB.Extensions;

public static class ServiceCollectionExtensions
{
    public static readonly ILoggerFactory MyLoggerFactory
        = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter((category, level) =>
                    category == DbLoggerCategory.Database.Command.Name
                    && level == LogLevel.Information)
                .AddConsole();
        });

    public static void AddRelationalDbContext(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is either null or empty.");
        }

        services.AddDbContext<CleanHrDbContext>(options =>
        {
            options.UseLoggerFactory(MyLoggerFactory);
            options.EnableSensitiveDataLogging(true);
            options.UseSqlServer(connectionString, builder =>
            {
                ////builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
                builder.MigrationsAssembly("CleanHr.DepartmentApi.Persistence.RelationalDB");
                builder.MigrationsHistoryTable("__EFCoreMigrationsHistory", schema: "_Migration");
            });
        });

        services.AddScoped<IDepartmentRepository, DepartmentRepository>();

        services.AddGenericRepository<CleanHrDbContext>();
        services.AddQueryRepository<CleanHrDbContext>();
    }
}
