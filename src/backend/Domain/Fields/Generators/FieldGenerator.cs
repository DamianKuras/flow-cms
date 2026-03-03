namespace Domain.Fields.Generators;

/// <summary>
/// Represents a generator that creates new field values derived from existing fields.
/// </summary>
public abstract class FieldGenerator
{
    /// <summary>
    /// Generates a new field value based on source field value.
    /// </summary>
    /// <param name="sourceValue">The source field value to generate from.</param>
    /// <returns>The generated field value.</returns>
    public abstract object? GenerateValue(object? sourceValue);
}
