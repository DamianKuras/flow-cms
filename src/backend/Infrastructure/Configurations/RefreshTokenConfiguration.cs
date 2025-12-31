using Domain.Users;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="RefreshToken"/> entity.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <summary>
    /// Configures the entity mapping for refresh tokens.
    /// </summary>
    /// <param name="builder">The entity type builder for RefreshToken.</param>
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token).IsRequired().HasMaxLength(512);

        builder.Property(rt => rt.UserId).IsRequired();

        builder.Property(rt => rt.CreatedOnUtc).IsRequired();

        builder.Property(rt => rt.ExpiresOnUtc).IsRequired();

        builder.Property(rt => rt.IsRevoked).IsRequired().HasDefaultValue(false);

        builder.Property(rt => rt.RevokedOnUtc).IsRequired(false);

        builder.HasIndex(rt => rt.Token).IsUnique();

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
