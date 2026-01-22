using Domain.Common;
using Domain.Fields.Transformers;
using Domain.Fields.Validations;

namespace Domain.Fields;

/// <summary>
/// Represents a configurable field with type constraints, validation and transformation rules.
/// </summary>
public class Field
{
    private Field() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Field"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the field.</param>
    /// <param name="type">The data type of the field.</param>
    /// <param name="name">The name of the field. Cannot be null or whitespace.</param>
    /// <param name="isRequired">Indicates whether the field is required.</param>
    /// <param name="validationRules">Optional collection of validation rules to apply to field values.</param>
    /// <param name="fieldTransformers">Optional collection of transformation rules to apply to field values.</param>
    /// <exception cref="ArgumentException">Thrown when name is null or whitespace.</exception>
    public Field(
        Guid id,
        FieldTypes type,
        string name,
        bool isRequired,
        IEnumerable<IValidationRule>? validationRules = null,
        IEnumerable<ITransformationRule>? fieldTransformers = null
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Field name cannot be null or whitespace.", nameof(name));
        }

        Id = id;
        Type = type;
        Name = name;
        IsRequired = isRequired;
        if (validationRules is not null)
        {
            ValidationRules = validationRules.ToList();
        }

        if (fieldTransformers is not null)
        {
            FieldTransformers = fieldTransformers.ToList();
        }
    }

    /// <summary>
    /// Gets the unique identifier for this field.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the name of the field.
    /// </summary>
    public string Name { get; init; } = default!;

    /// <summary>
    /// Gets the data type of the field.
    /// </summary>
    public FieldTypes Type { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this field is required.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Gets the collection of validation rules applied to this field.
    /// </summary>
    public IReadOnlyList<IValidationRule> ValidationRules { get; private set; } = [];

    /// <summary>
    /// Gets the collection of transformation rules applied to this field.
    /// </summary>
    public IReadOnlyList<ITransformationRule> FieldTransformers { get; private set; } = [];

    /// <summary>
    /// Validates the provided value against the field's type constraints and validation rules.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> containing any validation errors encountered.</returns>
    public ValidationResult Validate(object? value)
    {
        var validationResult = new ValidationResult(fieldName: Name);
        var sink = new ValidationErrorSink(validationResult);

        if (value is null)
        {
            return validationResult;
        }

        switch (Type)
        {
            case FieldTypes.Text:
            case FieldTypes.Richtext:
            case FieldTypes.Markdown:
                if (value is not string)
                {
                    validationResult.AddError($"Field '{Name}' must be a string.");
                }
                break;

            case FieldTypes.Numeric:
                // Allow numeric types or strings that parse to number.
                if (
                    value is not sbyte
                    && value is not byte
                    && value is not short
                    && value is not ushort
                    && value is not int
                    && value is not uint
                    && value is not long
                    && value is not ulong
                    && value is not float
                    && value is not double
                    && value is not decimal
                )
                {
                    // Try parsing if it's string.
                    if (value is string s && !decimal.TryParse(s, out _))
                    {
                        validationResult.AddError($"Field '{Name}' must be numeric.");
                    }
                    else if (value is not string)
                    {
                        validationResult.AddError($"Field '{Name}' must be numeric.");
                    }
                }
                break;

            case FieldTypes.Boolean:
                if (value is not bool)
                {
                    // Also allow string parsable to bool.
                    if (value is string boolStr && !bool.TryParse(boolStr, out _))
                    {
                        validationResult.AddError($"Field '{Name}' must be a boolean.");
                    }
                    else if (value is not string)
                    {
                        validationResult.AddError($"Field '{Name}' must be a boolean.");
                    }
                }
                break;

            default:
                // Just in case you extend the enum later
                validationResult.AddError($"Field '{Name}' has unsupported type '{Type}'.");
                break;
        }

        foreach (IValidationRule rule in ValidationRules)
        {
            rule.Validate(value!, sink);
        }

        return validationResult;
    }

    /// <summary>
    /// Assigns validation rules to this field.
    /// Intended for use by the infrastructure layer during entity hydration from persistence.
    /// </summary>
    /// <param name="rules">The validation rules to assign.</param>
    public void SetValidationRules(IEnumerable<IValidationRule> rules) =>
        ValidationRules = rules.ToList();

    /// <summary>
    /// Assigns transformation rules to this field.
    /// Intended for use by the infrastructure layer during entity hydration from persistence.
    /// </summary>
    /// <param name="fieldTransformers">The transformation rules to assign.</param>
    public void SetTransformationRules(IEnumerable<ITransformationRule> fieldTransformers) =>
        FieldTransformers = fieldTransformers.ToList();

    /// <summary>
    /// Applies all configured transformation rules to the provided value in sequence.
    /// </summary>
    /// <param name="value">The value to transform.</param>
    /// <returns>The transformed value after all transformers have been applied.</returns>
    public object? ApplyTransformers(object? value)
    {
        object? transformed = value;
        foreach (ITransformationRule transformer in FieldTransformers)
        {
            transformed = transformer.ApplyTransformation(transformed);
        }
        return transformed;
    }
}
