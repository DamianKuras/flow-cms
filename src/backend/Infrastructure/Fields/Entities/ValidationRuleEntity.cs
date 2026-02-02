namespace Infrastructure.Fields.Entities;

/// <summary>
/// Entity representing a validation rule applied to a field within a content type definition.
/// </summary>
public class ValidationRuleEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this validation rule entity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the field to which this validation rule applies.
    /// </summary>
    public Guid FieldId { get; set; }

    /// <summary>
    /// Gets or sets the type identifier for this validation rule.
    /// </summary>
    public string Type { get; set; } = default!;

    /// <summary>
    /// Gets or sets the collection of parameters that configure the behavior of this validation rule.
    /// </summary>
    public List<ValidationRuleParameterEntity> Parameters { get; set; } = new();
}
