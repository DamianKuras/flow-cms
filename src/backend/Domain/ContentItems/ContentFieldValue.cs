using System.Text.Json;

namespace Domain.ContentItems;

/// <summary>
/// Represents a value assigned to a content field within a content item, including metadata about creation and modification.
/// </summary>
/// <param name="value">The actual field value. Can be null for optional fields or fields that haven't been set.</param>
public class ContentFieldValue(object? value)
{
    /// <summary>
    /// Gets or sets the actual value stored in this field.
    /// </summary>
    /// <value>
    /// The field value as an <see cref="object"/>. Can be null for optional fields or unset values.
    /// The actual type depends on the field definition in the content type.
    /// </value>
    public object? Value { get; internal set; } = value;

    /// <summary>
    /// Gets or sets the UTC timestamp when this field value was created.
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when this field value was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; internal set; } = DateTime.UtcNow;

    /// <summary>
    /// Sets the value of this ContentFieldValue and updates timestamp.
    /// </summary>
    /// <param name="value">New value to set.</param>
    public void SetValue(object? value)
    {
        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }
}
