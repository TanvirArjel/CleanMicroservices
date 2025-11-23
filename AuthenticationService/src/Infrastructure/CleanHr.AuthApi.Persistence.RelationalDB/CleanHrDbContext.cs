using System;
using System.Threading;
using System.Threading.Tasks;
using CleanHr.AuthApi.Domain.Aggregates;
using CleanHr.AuthApi.Persistence.RelationalDB.EntityConfigurations.IdentityAggregate;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CleanHr.AuthApi.Persistence.RelationalDB;

internal sealed class CleanHrDbContext(DbContextOptions<CleanHrDbContext> options) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationUserConfiguration).Assembly);
        ////modelBuilder.ApplyBaseEntityConfiguration(); // This should be called after calling the derived entity configurations
    }
}
