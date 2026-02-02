namespace Domain.Fields;

/// <summary>
/// Enumeration of supported field data types for content type definitions.
/// </summary>
public enum FieldTypes
{
    /// <summary>
    /// Indicates a plain text field with no formatting or markup.
    /// </summary>
    Text,

    /// <summary>
    /// Indicates a numeric field supporting integer or decimal values.
    /// </summary>
    Numeric,

    /// <summary>
    /// Indicates a boolean field representing true or false values.
    /// </summary>
    Boolean,

    /// <summary>
    /// Indicates a rich text field supporting formatted content with HTML markup.
    /// </summary>
    Richtext,

    /// <summary>
    /// Indicates a markdown field supporting content formatted in Markdown syntax.
    /// </summary>
    Markdown,
}

/// <summary>
/// Provides utility methods for working with field types and their default values.
/// </summary>
public static class FieldTypeDefaults
{
    /// <summary>
    /// Retrieves the default value for a given field type.
    /// </summary>
    /// <param name="type">The field type for which to retrieve the default value.</param>
    /// <returns>
    /// The default value appropriate for the field type: false for Boolean, 0 for Numeric,
    /// and an empty string for Text, Markdown, and Richtext fields.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when an unsupported or unknown field type is provided.
    /// </exception>
    public static object? GetDefaultValue(FieldTypes type) =>
        type switch
        {
            FieldTypes.Boolean => false,
            FieldTypes.Numeric => 0,
            FieldTypes.Text or FieldTypes.Markdown or FieldTypes.Richtext => "",
            _ => throw new ArgumentOutOfRangeException(
                nameof(type),
                $"Unexpected field type: {type}"
            ),
        };
}
