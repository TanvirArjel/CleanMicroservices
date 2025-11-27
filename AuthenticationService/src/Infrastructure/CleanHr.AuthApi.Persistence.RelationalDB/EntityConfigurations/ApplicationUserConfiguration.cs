using CleanHr.AuthApi.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanHr.AuthApi.Persistence.RelationalDB.EntityConfigurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        ////builder.Property<int>("IdentityKey").ValueGeneratedOnAdd();
        builder.Property(au => au.UserName).HasMaxLength(50).IsRequired();
        builder.Property(au => au.NormalizedUserName).HasMaxLength(50).IsRequired();
        builder.Property(au => au.Email).HasMaxLength(50).IsRequired();
        builder.Property(au => au.NormalizedEmail).HasMaxLength(50).IsRequired();
        builder.Property(au => au.PhoneNumber).HasMaxLength(15).IsRequired(false);

        builder.HasIndex(au => au.Email).IsUnique(true);
        builder.HasIndex(au => au.NormalizedEmail).IsUnique(true);
    }
}
