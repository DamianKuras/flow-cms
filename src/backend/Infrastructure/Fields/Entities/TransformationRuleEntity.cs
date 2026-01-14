namespace Infrastructure.Fields.Entities;

/// <summary>
/// Represents a transformation rule entity that can be persisted in database.
/// Defines the type of transformation and its configuration parameters.
/// </summary>
public class TransformationRuleEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the transformation rule.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the field this transformation applies to.
    /// </summary>
    public Guid FieldId { get; set; }

    /// <summary>
    /// Gets or sets the type of transformation (e.g., "Trim", "Uppercase", "Replace").
    /// </summary>
    public string Type { get; set; } = default!;

    /// <summary>
    /// Gets or sets the collection of parameters that configure this transformation.
    /// </summary>
    public List<TransformationRuleParameterEntity> Parameters { get; set; } = new();
}
