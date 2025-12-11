using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CleanHr.DepartmentApi.Persistence.RelationalDB.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace CleanHr.DepartmentApi.Persistence.RelationalDB;

internal class CleanHrDbContext : DbContext
{
    public CleanHrDbContext(DbContextOptions<CleanHrDbContext> options) : base(options)
    {
    }

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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DepartmentConfiguration).Assembly);
        ////modelBuilder.ApplyBaseEntityConfiguration(); // This should be called after calling the derived entity configurations
    }
}
