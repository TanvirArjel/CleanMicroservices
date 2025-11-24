using CleanHr.AuthApi.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanHr.AuthApi.Persistence.RelationalDB.EntityConfigurations;

internal sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        ////builder.Property<int>("IdentityKey").ValueGeneratedOnAdd();
    }
}
