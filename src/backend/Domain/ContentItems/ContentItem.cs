using Domain.Common;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Validations;

namespace Domain.ContentItems;

public sealed class CannotPublishContentItemException(string message) : Exception(message);

/// <summary>
/// Represents a content item that manages a collection of field values based on a specific content type definition.
/// </summary>
public class ContentItem : ISoftDeletable
{
    /// <summary>
    /// Required by EF Core. Do not use directly.
    /// </summary>
    public ContentItem() { }

    /// <param name="id">Unique identifier.</param>
    /// <param name="title">Title of the content item. Cannot be null or empty.</param>
    /// <param name="contentTypeId">The content type this item belongs to.</param>
    /// <param name="version">Version number. Defaults to 0 for new drafts.</param>
    /// <param name="status">Publication status. Defaults to Draft.</param>
    public ContentItem(
        Guid id,
        string title,
        Guid contentTypeId,
        int version = 0,
        ContentItemStatus status = ContentItemStatus.Draft
    )
    {
        Id = id;
        Title = title;
        ContentTypeId = contentTypeId;
        Version = version;
        Status = status;
    }

    public Guid Id { get; }
    public string Title { get; } = "";
    public Guid ContentTypeId { get; private set; }
    public ContentItemStatus Status { get; private set; } = ContentItemStatus.Draft;
    public int Version { get; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedOnUtc { get; private set; }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedOnUtc = DateTime.UtcNow;
    }

    private readonly Dictionary<Guid, ContentFieldValue> _values = [];

    public IReadOnlyDictionary<Guid, ContentFieldValue> Values => _values.AsReadOnly();

    internal void SetInternalFieldValue(Guid fieldId, object? value)
    {
        if (_values.TryGetValue(fieldId, out ContentFieldValue? existing) && existing is not null)
        {
            existing.SetValue(value);
        }
        else
        {
            _values[fieldId] = new ContentFieldValue(value);
        }
    }

    /// <summary>
    /// Migrates this content item to a new schema version by pointing it at the new content type
    /// and filling in default values for any fields that exist in the new schema but are absent
    /// from this item's current values. Fields that no longer exist in the new schema are left
    /// intact in the values dictionary (they are simply ignored during reads).
    /// </summary>
    public void MigrateToSchema(ContentType newSchema)
    {
        ContentTypeId = newSchema.Id;
        foreach (Field field in newSchema.Fields)
        {
            if (!_values.ContainsKey(field.Id))
                _values[field.Id] = new ContentFieldValue(FieldTypeDefaults.GetDefaultValue(field.Type));
        }
    }

    /// <summary>
    /// Creates a new published version of this content item.
    /// The draft is unchanged; the returned instance is the new published row.
    /// </summary>
    /// <param name="previousPublished">
    /// The currently published version, if any. Used to determine the next version number.
    /// Will be soft-deleted by the caller after this method returns.
    /// </param>
    public ContentItem PublishFrom(ContentItem? previousPublished)
    {
        if (Status != ContentItemStatus.Draft)
        {
            throw new CannotPublishContentItemException(
                $"Only drafts can be published. Current status: {Status}."
            );
        }

        int nextVersion = (previousPublished?.Version ?? 0) + 1;

        ContentItem published = new(
            id: Guid.NewGuid(),
            title: Title,
            contentTypeId: ContentTypeId,
            version: nextVersion,
            status: ContentItemStatus.Published
        );

        foreach (KeyValuePair<Guid, ContentFieldValue> kv in _values)
        {
            published.SetInternalFieldValue(kv.Key, kv.Value.Value);
        }

        return published;
    }
}

/// <summary>
/// Represents a lightweight projection of a content item for paged lists and summary views.
/// </summary>
/// <param name="Id">The unique identifier of the content item.</param>
/// <param name="Title">The title of the content item.</param>
public record PagedContentItem(Guid Id, string Title);
