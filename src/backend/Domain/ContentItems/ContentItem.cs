using Domain.Common;
using Domain.ContentTypes;
using Domain.Fields;

namespace Domain.ContentItems;

/// <summary>
/// Represents a content item that manages a collection of field values based on a specific content type definition.
/// </summary>
/// <param name="id">The unique identifier for this content item.</param>
/// <param name="name">The name of the content item.</param>
/// <param name="content_type">The content type definition that defines which fields this item can have.</param>
/// <param name="values">The initial collection of field values.</param>
public class ContentItem(
    Guid id,
    string name,
    ContentType content_type,
    Dictionary<Guid, ContentFieldValue> values
)
{
    public Guid Id { get; } = id;

    public string Name { get; } = name;

    public ContentType ContentType { get; } = content_type;

    public ContentItemStatus Status { get; } = ContentItemStatus.Draft;
    private readonly Dictionary<Guid, ContentFieldValue> _values = values;

    public IReadOnlyDictionary<Guid, ContentFieldValue> Values =>
        _values.AsReadOnly();

    /// <summary>
    /// Sets or updates a field value for this content item after applying transformation
    /// and validation based on the field definition.
    /// </summary>
    /// <param name="definition">The field definition that specifies how to transform and validate the raw value.
    ///  The field must be defined in this item's ContentType.</param>
    /// <param name="rawValue">The raw field value to set. This can be any type and will be transformed
    /// according to the field's transformer pipeline.</param>
    /// <returns>
    /// A Result object that indicates success or contains validation error details.
    /// Success indicates the field value was successfully set.
    /// Failure contains details about why the operation failed.</returns>
    public Result SetFieldValue(Field definition, object? rawValue)
    {
        if (!ContentType.HasField(definition.Id))
        {
            return Result.Failure(
                Error.NotFound(
                    $"Field '{definition.Name}' not in ContentType '{ContentType.Name}'"
                )
            );
        }
        object? transformed = definition.ApplyTransformers(rawValue);

        ValidationResult validation_result = definition.Validate(transformed);
        if (!validation_result.IsValid)
        {
            return Result.Failure(Error.Validation([validation_result]));
        }

        _values[definition.Id].Value = transformed;
        _values[definition.Id].UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public Result<object> GetFieldValue(Guid fieldId)
    {
        if (!ContentType.HasField(fieldId))
        {
            return Result<object>.Failure(
                Error.NotFound(
                    $"Field with guid '{fieldId}' not in ContentType '{ContentType.Name}'"
                )
            );
        }
        return Result<object>.Success(Values[fieldId].Value);
    }
}
