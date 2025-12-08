using Domain.Fields;

namespace Application.ContentTypes;

/// <summary>
/// Data transfer object representing a field definition.
/// </summary>
/// <param name="Id">Unique identifier for this field.</param>
/// <param name="Name">Display name of the field.</param>
/// <param name="Type">The data type of this field (e.g., "Text", "Number", "DateTime").</param>
/// <param name="IsRequired">Indicates whether this field is required.</param>
/// <param name="ValidationRules">Optional collection of validation rules specific to this field.</param>
/// <param name="TransformationRules">Optional collection of transformation rules specific to this field.</param>
public record FieldDto(
    Guid Id,
    string Name,
    string Type,
    bool IsRequired,
    List<ValidationRuleDto>? ValidationRules = null,
    List<TransformationRuleDto>? TransformationRules = null
);

/// <summary>
/// Data transfer object representing a validation rule.
/// </summary>
/// <param name="Type">The type of validation rule.</param>
/// <param name="Parameters">Optional dictionary of configuration parameters for the validation rule. </param>
public record ValidationRuleDto(
    string Type,
    Dictionary<string, object>? Parameters
);

/// <summary>
/// Data transfer object representing a transformation rule.
/// </summary>
/// <param name="Type">The type of transformation rule</param>
/// <param name="Parameters">Optional dictionary of configuration parameters for the transformation rule.</param>
public record TransformationRuleDto(
    string Type,
    Dictionary<string, object> Parameters
);

/// <summary>
/// Data transfer object representing a content type with its configuration, fields, and validation rules.
/// </summary>
/// <param name="Status">The current status of the content type.</param>
/// <param name="Fields">A read-only collection of field definitions that make up this content type.</param>
/// <param name="Version">The version number of this content type, incremented with each modification.</param>
/// <param name="ValidationRules">Optional collection of validation rules that apply to the content type. Null if no global validation rules exist.</param>
/// <param name="TransformationRuleDto">Optional collection of transformation rules that apply to the entire content type. Null if no global validation rules exist.</param>
public record ContentTypeDto(
    string Status,
    IReadOnlyList<FieldDto> Fields,
    int Version,
    List<ValidationRuleDto>? ValidationRules = null,
    List<TransformationRuleDto>? TransformationRules = null
);

/// <summary>
/// Data transfer object for creating a new validation rule.
/// </summary>
public class CreateValidationRuleDto
{
    public required string Type { get; set; }
    public required Dictionary<string, object> Parameters { get; set; }
}

/// <summary>
/// Data transfer object for creating a new field within a content type.
/// </summary>
public class CreateFieldDto
{
    public required string Name { get; set; }
    public required FieldTypes Type { get; set; }
    public required bool IsRequired { get; set; }

    public List<CreateValidationRuleDto> ValidationRules { get; set; } = new();
}
