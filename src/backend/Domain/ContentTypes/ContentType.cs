using Domain.Common;
using Domain.Fields;

namespace Domain.ContentTypes;

/// <summary>Thrown when attempting to publish a content type that isn't in a valid state for publishing.</summary>
public sealed class CannotPublishContentTypeException : Exception
{
    public CannotPublishContentTypeException(string message)
        : base(message) { }
}

/// <summary>
/// Defines the structure and schema for content entries.
/// Exists as either a draft or a published version; name is the stable identifier across versions.
/// </summary>
public sealed class ContentType : ISoftDeletable
{
    private ContentType() { }

    public ContentType(
        Guid id,
        string name,
        IReadOnlyList<Field> fields,
        int version = 0,
        ContentTypeStatus status = ContentTypeStatus.DRAFT
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Content type name cannot be null or whitespace.", nameof(name));

        if (version < 0)
            throw new ArgumentOutOfRangeException(nameof(version), "Version cannot be negative.");

        Id = id;
        Name = name;
        _fields = fields.ToList();
        Version = version;
        Status = status;
    }

    public Guid Id { get; }
    public string Name { get; } = "";
    public ContentTypeStatus Status { get; private set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public int Version { get; }

    private List<Field> _fields = [];
    public IReadOnlyList<Field> Fields => _fields.AsReadOnly();

    private IReadOnlyDictionary<Guid, Field>? _fieldsById;
    public IReadOnlyDictionary<Guid, Field> FieldsById => _fieldsById ??= _fields.ToDictionary(f => f.Id);

    /// <inheritdoc/>
    public bool IsDeleted { get; private set; }

    /// <inheritdoc/>
    public DateTime? DeletedOnUtc { get; private set; }

    /// <inheritdoc/>
    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedOnUtc = DateTime.UtcNow;
    }

    public void Archive() => Status = ContentTypeStatus.ARCHIVE;

    /// <summary>
    /// Replaces the field set on a DRAFT content type. Pass the same tracked <see cref="Field"/> instances
    /// for existing fields so EF can detect property-level changes; omitted fields are deleted via cascade.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when called on a non-draft.</exception>
    public void UpdateFields(IReadOnlyList<Field> newFields)
    {
        if (Status != ContentTypeStatus.DRAFT)
            throw new InvalidOperationException("Only draft content types can have their fields updated.");
        _fieldsById = null;
        _fields.Clear();
        _fields.AddRange(newFields);
    }

    public bool HasField(Guid fieldId) => FieldsById.ContainsKey(fieldId);

    /// <summary>
    /// Creates a new published version from this draft.
    /// Pass the previous published version to determine the next version number; null starts at 1.
    /// </summary>
    /// <exception cref="CannotPublishContentTypeException">Thrown when this content type is not in DRAFT status.</exception>
    public ContentType PublishFrom(ContentType? previousPublished)
    {
        if (Status != ContentTypeStatus.DRAFT)
            throw new CannotPublishContentTypeException("Only drafts can be published.");

        int nextVersion = (previousPublished?.Version ?? 0) + 1;

        // Each field must be a new instance with a new ID so EF inserts separate rows
        // for the published type without reassigning the draft's field rows.
        List<Field> copiedFields = _fields
            .Select(f => new Field(
                id: Guid.NewGuid(),
                type: f.Type,
                name: f.Name,
                isRequired: f.IsRequired,
                validationRules: f.ValidationRules,
                fieldTransformers: f.FieldTransformers
            ))
            .ToList();

        return new ContentType(
            id: Guid.NewGuid(),
            name: Name,
            fields: copiedFields,
            version: nextVersion,
            status: ContentTypeStatus.PUBLISHED
        );
    }
}

/// <summary>Lightweight projection of a content type for paged list views.</summary>
public record PagedContentType(
    Guid Id,
    string Name,
    string Status,
    int Version,
    DateTime CreatedAt
);

/// <summary>
/// One entry per content type name, carrying the row IDs of the current
/// published and draft versions (either may be null).
/// </summary>
public record ContentTypeNameSummary(
    string Name,
    Guid? PublishedId,
    int? PublishedVersion,
    Guid? DraftId
);
