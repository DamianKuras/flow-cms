using System;

namespace Infrastructure.Fields.Entities;

/// <summary>
/// Represents a configuration parameter for a transformation rule.
/// Stores key-value pairs with type information for type-safe parameter handling.
/// </summary>
public class TransformationRuleParameterEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the parameter.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the transformation rule this parameter belongs to.
    /// </summary>
    public Guid TransformationRuleId { get; set; }

    /// <summary>
    /// Gets or sets the parameter key.
    /// </summary>
    public string Key { get; set; } = default!;

    /// <summary>
    /// Gets or sets the parameter value as a string representation.
    /// </summary>
    public string Value { get; set; } = default!;

    /// <summary>
    /// Gets or sets the data type of the parameter value.
    /// </summary>
    public string ValueType { get; set; } = default!;
}
