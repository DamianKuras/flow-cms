using Domain.Common;

namespace Domain.Fields.Validations;

/// <summary>
/// Represents a base class for validation rules that can be applied to a field.
/// </summary>
public abstract class ValidationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Field Field { get; set; } = default!;
    public abstract Capability RequiredCapability { get; }

    /// <summary>
    /// Validates the given value against this validation rule.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>A <see cref="Result"/> containing a success state if validation passes,
    /// or a failure state with an <see cref="Error"/> describing the validation failure.</returns>
    public abstract Result Validate(object value);
};
