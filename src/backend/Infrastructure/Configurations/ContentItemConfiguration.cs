using System.Text.Json;
using Domain.ContentItems;
using Domain.ContentTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

internal class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.ToTable("content_items");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title).HasColumnName("title");

        builder.Property(c => c.ContentTypeId).HasColumnName("content_type_id");

        builder.Property(c => c.Status).HasColumnName("status");

        builder
            .Property(c => c.Values)
            .HasColumnType("jsonb")
            .HasConversion(
                // To database: serialize the dictionary
                v =>
                    JsonSerializer.Serialize(
                        v.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value.Value),
                        (JsonSerializerOptions?)null
                    ),
                // From database: deserialize to dictionary
                v =>
                    JsonSerializer
                        .Deserialize<Dictionary<string, JsonElement>>(
                            v,
                            (JsonSerializerOptions?)null
                        )!
                        .ToDictionary(
                            kvp => Guid.Parse(kvp.Key),
                            kvp => new ContentFieldValue(kvp.Value)
                        )
            );

        builder.Property(c => c.Version).HasColumnName("version").HasDefaultValue(0);

        builder.Property(c => c.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);

        builder.Property(c => c.DeletedOnUtc).HasColumnName("deleted_on_utc");

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder
            .HasOne<ContentType>()
            .WithMany()
            .HasForeignKey(c => c.ContentTypeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
