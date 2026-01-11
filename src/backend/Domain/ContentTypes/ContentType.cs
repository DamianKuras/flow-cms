using Domain.Common;
using Domain.Fields;

namespace Domain.ContentTypes;

/// <summary>
/// Exception thrown when attempting to publish a content type that is not in a valid state for publishing.
/// </summary>
public class CannotPublishContentTypeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CannotPublishContentTypeException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public CannotPublishContentTypeException(string message)
        : base(message) { }
}

/// <summary>
/// Represents a content type definition that defines the structure and schema for content entries.
/// Content types can exist in draft or published states and are versioned to track changes over time.
/// </summary>
public class ContentType : ISoftDeletable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentType"/> class.
    /// This parameterless constructor is required for Entity Framework Core and should not be used directly.
    /// </summary>
    private ContentType() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentType"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the content type.</param>
    /// <param name="name">The name of the content type. Must not be null or whitespace.</param>
    /// <param name="fields">The collection of fields that define the content type schema.</param>
    /// <param name="version">The version number of the content type. Defaults to 0 for new drafts.</param>
    /// <param name="status">The publication status of the content type. Defaults to DRAFT.</param>
    /// <exception cref="ArgumentException">Thrown when name is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when fields is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when version is negative.</exception>
    public ContentType(
        Guid id,
        string name,
        IReadOnlyList<Field> fields,
        int version = 0,
        ContentTypeStatus status = ContentTypeStatus.DRAFT
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Content type name cannot be null or whitespace.",
                nameof(name)
            );
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version cannot be negative.");
        }

        Id = id;
        Name = name;
        Fields = fields;
        Version = version;
        Status = status;
    }

    /// <summary>
    /// Gets the unique identifier for this content type.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the name of the content type.
    /// </summary>
    public string Name { get; } = "";

    /// <summary>
    /// Gets the current publication status of the content type.
    /// </summary>
    public ContentTypeStatus Status { get; }

    /// <summary>
    /// Gets the ordered collection of fields that define the schema for this content type.
    /// </summary>
    public IReadOnlyList<Field> Fields { get; }

    private IReadOnlyDictionary<Guid, Field>? _fieldsById;

    /// <summary>
    /// Gets a dictionary that provides fast lookup of fields by their unique identifier.
    /// </summary>
    public IReadOnlyDictionary<Guid, Field> FieldsById =>
        _fieldsById ??= Fields.ToDictionary(f => f.Id);

    /// <summary>
    /// Gets the UTC timestamp when this content type was created.
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the version number of this content type.
    /// </summary>
    public int Version { get; }

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

    /// <summary>
    /// Determines whether this content type contains a field with the specified identifier.
    /// </summary>
    /// <param name="fieldId">The unique identifier of the field to locate.</param>
    /// <returns>
    /// <c>true</c> if the content type contains a field with the specified identifier; otherwise, <c>false</c>.
    /// </returns>
    public bool HasField(Guid fieldId) => FieldsById.ContainsKey(fieldId);

    /// <summary>
    /// Creates a new published version of this content type based on the current draft.
    /// </summary>
    /// <param name="previousPublished">
    /// The previously published version of this content type, if any.
    /// Used to determine the next version number. If null, the new version will be 1.
    /// </param>
    /// <returns>
    /// A new <see cref="ContentType"/> instance with PUBLISHED status and an incremented version number.
    /// </returns>
    /// <exception cref="CannotPublishContentTypeException">
    /// Thrown when attempting to publish a content type that is not in DRAFT status.
    /// </exception>
    public ContentType PublishFrom(ContentType? previousPublished)
    {
        if (Status != ContentTypeStatus.DRAFT)
        {
            throw new CannotPublishContentTypeException("Only drafts can be published.");
        }

        int nextVersion = (previousPublished?.Version ?? 0) + 1;

        return new ContentType(
            id: Guid.NewGuid(),
            name: Name,
            fields: Fields.ToList().AsReadOnly(),
            version: nextVersion,
            status: ContentTypeStatus.PUBLISHED
        );
    }
}

/// <summary>
/// Represents a lightweight projection of a content type for paged list views.
/// </summary>
/// <param name="Id">The unique identifier of the content type.</param>
/// <param name="Name">The name of the content type.</param>
/// <param name="Status">The publication status as a string (e.g., "DRAFT", "PUBLISHED").</param>
/// <param name="Version">The version number of the content type.</param>
public record PagedContentType(Guid Id, string Name, string Status, int Version);
