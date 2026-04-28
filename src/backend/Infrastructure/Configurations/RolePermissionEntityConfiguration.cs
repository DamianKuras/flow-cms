using Infrastructure.Persistence.Permissions;
using Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

/// <summary>EF Core configuration for <see cref="RolePermissionEntity"/>.</summary>
public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermissionEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RolePermissionEntity> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.Id).ValueGeneratedOnAdd();

        builder.Property(rp => rp.Action).HasConversion<int>().IsRequired();

        builder.Property(rp => rp.ResourceType).HasConversion<int>().IsRequired();

        builder.Property(rp => rp.Scope).HasConversion<int>().IsRequired();

        builder.Property(rp => rp.ResourceId).HasMaxLength(256).IsRequired(false);

        builder
            .HasIndex(rp => new { rp.RoleId, rp.Action, rp.ResourceType, rp.ResourceId })
            .IsUnique()
            .AreNullsDistinct(false);

        builder
            .HasOne<AppRole>()
            .WithMany()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
