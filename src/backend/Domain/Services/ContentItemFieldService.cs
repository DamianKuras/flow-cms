using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Fields;

namespace Domain.Services;

/// <summary>
/// Provides services for managing field values on content items,
/// including validation and transformation operations.
/// </summary>
public static class ContentItemFieldService
{
    /// <summary>
    /// Sets a field value on a content item after applying transformations and validation.
    /// </summary>
    /// <param name="item">The content item to update.</param>
    /// <param name="type">The content type definition containing field metadata.</param>
    /// <param name="fieldId">The unique identifier of the field to set.</param>
    /// <param name="rawValue">The raw value to set. May be null if the field allows null values.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure.
    /// Returns <see cref="Result.Failure"/> if the field doesn't exist in the content type.
    /// Returns <see cref="Result.FieldValidationFailure"/> if validation fails after transformation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="item"/> or <paramref name="type"/> is null.
    /// </exception>
    public static Result SetValue(
        ContentItem item,
        ContentType type,
        Guid fieldId,
        object? rawValue
    )
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        if (!type.HasField(fieldId))
        {
            return Result.Failure(
                Error.NotFound($"Field '{fieldId}' not in ContentType '{type.Name}'")
            );
        }

        Field field = type.FieldsById[fieldId]!; // Safe after HasField check.
        object? transformedValue;
        try
        {
            transformedValue = field.ApplyTransformers(rawValue);
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Infrastructure($"Transformation failed for field '{fieldId}': {ex.Message}")
            );
        }
        ValidationResult validation = field.Validate(transformedValue);
        if (validation.IsInvalid)
        {
            return Result.FieldValidationFailure(validation);
        }

        item.SetInternalFieldValue(fieldId, transformedValue);
        return Result.Success();
    }
}
