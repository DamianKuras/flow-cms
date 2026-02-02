using System;

namespace Infrastructure.Fields.Entities;

/// <summary>
/// Entity representing a single parameter that configures a validation rule's behavior.
/// </summary>
public class ValidationRuleParameterEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this validation rule parameter entity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the validation rule to which this parameter belongs.
    /// </summary>
    public Guid ValidationRuleId { get; set; }

    /// <summary>
    /// Gets or sets the parameter name or identifier that specifies which aspect of the validation rule this parameter configures.
    /// </summary>
    public string Key { get; set; } = default!;

    /// <summary>
    /// Gets or sets the parameter value as a string representation.
    /// </summary>
    public string Value { get; set; } = default!;

    /// <summary>
    /// Gets or sets the value type that specifies how to deserialize the Value property.
    /// </summary>
    public string ValueType { get; set; } = default!;
}
