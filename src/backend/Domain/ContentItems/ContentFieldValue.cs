namespace Domain.ContentItems;

/// <summary>
/// Represents a value assigned to a content field within a content item.
/// </summary>
/// <param name="value">The actual field value. Can be null.</param>
public class ContentFieldValue(object? value)
{
    public object? Value { get; set; } = value;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}
