using Domain.Fields;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

internal class FieldConfiguration : IEntityTypeConfiguration<Field>
{
    public void Configure(EntityTypeBuilder<Field> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property<Guid>("ContentTypeId").IsRequired();

        builder
            .Property<string>("ValidationRulesJson")
            .HasColumnName("ValidationRules")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder
            .Property<string>("TransformationRulesJson")
            .HasColumnName("TransformationRules")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Ignore(c => c.ValidationRules);
        builder.Ignore(c => c.FieldTransformers);

        builder.HasIndex("ContentTypeId");
    }
}
