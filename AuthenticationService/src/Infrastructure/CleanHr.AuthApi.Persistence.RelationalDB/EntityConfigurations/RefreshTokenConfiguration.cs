using CleanHr.AuthApi.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanHr.AuthApi.Persistence.RelationalDB.EntityConfigurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.Property(rt => rt.TokenFamilyId)
            .IsRequired();

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(rt => rt.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rt => rt.RevokedAtUtc);

        builder.Property(rt => rt.UsedAtUtc);

        builder.Property(rt => rt.ExpireAtUtc)
            .IsRequired();

        builder.Property(rt => rt.CreatedAtUtc)
            .HasDefaultValueSql("getutcdate()");

        builder.HasIndex(rt => new { rt.UserId, rt.Token })
            .IsUnique();

        builder.HasIndex(rt => rt.TokenFamilyId);

        builder.HasOne(rt => rt.ApplicationUser)
            .WithMany(au => au.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
