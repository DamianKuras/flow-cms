using Domain.Common;
using Domain.ContentTypes;
using Domain.Fields;
// using Domain.Fields.Transformers;
using Domain.Fields.Validations;

namespace Domain.ContentItems;

/// <summary>
/// Represents a content item that manages a collection of field values based on a specific content type definition.
/// </summary>
public class ContentItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentItem"/> class.
    /// </summary>
    public ContentItem() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentItem"/> class with the specified values.
    /// </summary>
    /// <param name="id">The unique identifier for the content item.</param>
    /// <param name="title">The title of the content item. Cannot be null or empty.</param>
    /// <param name="contentTypeId">The unique identifier of the content type this item belongs to.</param>
    /// <remarks>
    /// The content item is created with a default status of <see cref="ContentItemStatus.Draft"/>.
    /// Field values should be set using the content item field service after construction.
    /// </remarks>
    public ContentItem(Guid id, string title, Guid contentTypeId)
    {
        Id = id;
        Title = title;
        ContentTypeId = contentTypeId;
    }

    /// <summary>
    /// Gets the unique identifier for this content item.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the title of the content item.
    /// </summary>
    public string Title { get; } = "";

    /// <summary>
    /// Gets the unique identifier of the content type this item is based on.
    /// </summary>
    /// <value>A <see cref="Guid"/> referencing the associated <see cref="ContentType"/>.</value>
    public Guid ContentTypeId { get; private set; }

    /// <summary>
    /// Gets the current status of the content item.
    /// </summary>
    /// <value>The status of the content item, defaulting to <see cref="ContentItemStatus.Draft"/>.</value>
    public ContentItemStatus Status { get; } = ContentItemStatus.Draft;

    private readonly Dictionary<Guid, ContentFieldValue> _values = [];

    /// <summary>
    /// Gets a read-only dictionary of field values keyed by field ID.
    /// </summary>
    /// <value>
    /// A read-only dictionary where keys are field IDs and values are <see cref="ContentFieldValue"/> instances.
    /// </value>
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
}

/// <summary>
/// Represents a lightweight projection of a content item for paged lists and summary views.
/// </summary>
/// <param name="Id">The unique identifier of the content item.</param>
/// <param name="Title">The title of the content item.</param>
public record PagedContentItem(Guid Id, string Title);
