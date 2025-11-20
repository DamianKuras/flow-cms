using Domain.Fields;

namespace Domain.ContentTypes;

/// <summary>
/// Represents a content type definition that specifies the schema and metadata for content items.
/// </summary>
/// <param name="Id">The unique identifier for this content type.</param>
/// <param name="Name">The name of this content type.</param>
/// <param name="FieldsList">The collection of field definitions that define this content type's schema.</param>
public record ContentType(Guid Id, string Name, IEnumerable<Field> FieldsList)
{
    public ContentTypeStatus Status { get; }
    public IReadOnlyDictionary<Guid, Field> Fields { get; } =
        FieldsList.ToDictionary(f => f.Id);
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public int Version { get; }

    public bool HasField(Guid field_id) => Fields.ContainsKey(field_id);
}
