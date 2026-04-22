using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

internal class MigrationJobConfiguration : IEntityTypeConfiguration<MigrationJob>
{
    public void Configure(EntityTypeBuilder<MigrationJob> builder)
    {
        builder.ToTable("migration_jobs");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.FromSchemaId).HasColumnName("from_schema_id").IsRequired();
        builder.Property(j => j.ToSchemaId).HasColumnName("to_schema_id").IsRequired();
        builder.Property(j => j.Mode).HasColumnName("mode");
        builder.Property(j => j.Status).HasColumnName("status");
        builder.Property(j => j.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(j => j.CreatedAt).HasColumnName("created_at");
        builder.Property(j => j.TotalItemsCount).HasColumnName("total_items_count").HasDefaultValue(0);
        builder.Property(j => j.MigratedItemsCount).HasColumnName("migrated_items_count").HasDefaultValue(0);
        builder.Property(j => j.FailedItemsCount).HasColumnName("failed_items_count").HasDefaultValue(0);
    }
}
