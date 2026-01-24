using Domain.Fields;

namespace Application.ContentTypes;

/// <summary>
/// Data transfer object representing a validation rule.
/// </summary>
/// <param name="Type">The type of validation rule.</param>
/// <param name="Parameters">Optional dictionary of configuration parameters for the validation rule. </param>
public record ValidationRuleDto(string Type, Dictionary<string, object>? Parameters);

/// <summary>
/// Data transfer object representing a transformation rule.
/// </summary>
/// <param name="Type">The type of transformation rule</param>
/// <param name="Parameters">Optional dictionary of configuration parameters for the transformation rule.</param>
public record TransformationRuleDto(string Type, Dictionary<string, object>? Parameters);

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
/// Data transfer object for creating a new validation rule.
/// </summary>
/// <param name="Type"></param>
/// <param name="Parameters"></param>
public record CreateValidationRuleDto(string Type, Dictionary<string, object>? Parameters);

/// <summary>
///
/// </summary>
/// <param name="Type"></param>
/// <param name="Parameters"></param>
public record CreateTransformationRuleDto(string Type, Dictionary<string, object>? Parameters);

/// <summary>
/// Data transfer object representing a content type with its configuration, fields, and validation rules.
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="Status">The current status of the content type.</param>
/// <param name="Fields">A read-only collection of field definitions that make up this content type.</param>
/// <param name="Version">The version number of this content type, incremented with each modification.</param>
public record ContentTypeDto(
    Guid Id,
    string Name,
    string Status,
    IReadOnlyList<FieldDto> Fields,
    int Version
);

/// <summary>
/// Data transfer object for creating a new field within a content type.
/// </summary>
public record CreateFieldDto(
    string Name,
    FieldTypes Type,
    bool IsRequired,
    List<CreateValidationRuleDto>? ValidationRules = null,
    List<CreateTransformationRuleDto>? TransformationRules = null
);
