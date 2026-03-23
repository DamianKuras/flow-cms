using Domain.ContentTypes;
using Domain.Fields;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

internal class ContentTypeConfiguration : IEntityTypeConfiguration<ContentType>
{
    public void Configure(EntityTypeBuilder<ContentType> builder)
    {
        builder.ToTable("content_types");
        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.Name).IsRequired();
        builder.Property(ct => ct.Status);
        builder.Property(ct => ct.CreatedAt);
        builder.Property(ct => ct.Version);

        builder
            .HasMany(x => x.Fields)
            .WithOne()
            .HasForeignKey("ContentTypeId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(ct => !ct.IsDeleted);
    }
}
