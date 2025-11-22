using Domain.Common;
using Domain.Fields.Transformers;
using Domain.Fields.Validations;

namespace Domain.Fields;

/// <summary>
/// Represents a field within a content type schema.
/// </summary>
/// <param name="id">The unique identifier for this field.</param>
/// <param name="type">The field type for this field.</param>
/// <param name="name">The name of the field.</param>
/// <param name="isRequired">Represents whether the value for this field is required.</param>
/// <param name="validationRules">List of the validation rules for this field, which may be empty.</param>
/// <param name="transformers">List of the transformations for this field, which may be empty.</param>
public class Field(
    Guid id,
    FieldTypes type,
    string name,
    bool isRequired,
    IEnumerable<ValidationRule> validationRules,
    IEnumerable<FieldTransformer> transformers
)
{
    public Guid Id { get; } = id;

    public FieldTypes Type { get; } = type;
    public string Name { get; } = name;

    public bool IsRequired { get; set; } = isRequired;

    public IReadOnlyList<ValidationRule> ValidationRules { get; set; } =
        validationRules.ToList();

    public IReadOnlyList<FieldTransformer> Transformers { get; set; } =
        transformers.ToList();

    /// <summary>
    /// Applies transformers sequentially to input value.
    /// </summary>
    /// <param name="value">The value to transform.</param>
    /// <returns>The result after all transformers are applied sequentially to the value.</returns>
    public object? ApplyTransformers(object? value)
    {
        object? result = value;

        foreach (var transformer in Transformers)
            result = transformer.ApplyTransformation(result);

        return result;
    }

    /// <summary>
    /// Validates an input value using this field's rules.
    /// Returns ValidationError containing ValidationFailure for this field.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> containing validation failures for
    /// <paramref name="value"/>, or an empty result if valid.</returns>
    public ValidationResult Validate(object? value)
    {
        ValidationResult validationResult = new(FieldName: Name);
        if (IsRequired && value is null)
        {
            validationResult.AddError("Field is required!");
            return validationResult;
        }
        value ??= FieldTypeDefaults.GetDefaultValue(Type);
        foreach (var rule in ValidationRules)
        {
            Result rule_validation_result = rule.Validate(value!);
            if (rule_validation_result.IsFailure)
            {
                validationResult.AddErrors(
                    rule_validation_result.Errors!.Select(e => e.Message)
                );
            }
        }
        return validationResult;
    }
}
