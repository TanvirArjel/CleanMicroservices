using System;
using System.Threading;
using System.Threading.Tasks;
using CleanHr.EmployeeApi.Persistence.RelationalDB.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace CleanHr.EmployeeApi.Persistence.RelationalDB;

internal sealed class CleanHrDbContext(DbContextOptions<CleanHrDbContext> options) : DbContext(options)
{
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ////ChangeTracker.ApplyValueGenerationOnUpdate();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ////ChangeTracker.ApplyValueGenerationOnUpdate();
        return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmployeeConfiguration).Assembly);
        ////modelBuilder.ApplyBaseEntityConfiguration(); // This should be called after calling the derived entity configurations
    }
}
