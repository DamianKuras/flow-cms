using Application.ContentTypes;
using Domain.Fields;

namespace Integration.Tests.Builders;

public sealed class FieldBuilder
{
    private readonly string _name;
    private readonly FieldTypes _type;
    private bool _required;
    private readonly List<CreateValidationRuleDto> _validationRules = [];
    private readonly List<CreateTransformationRuleDto> _transformationRules = [];

    internal FieldBuilder(string name, FieldTypes type)
    {
        _name = name;
        _type = type;
    }

    public FieldBuilder Required()
    {
        _required = true;
        return this;
    }

    public FieldBuilder Optional()
    {
        _required = false;
        return this;
    }

    public FieldBuilder WithValidationRule(
        string type,
        Dictionary<string, object>? parameters = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        _validationRules.Add(new CreateValidationRuleDto(type, parameters));
        return this;
    }

    public FieldBuilder WithTransformationRule(
        string type,
        Dictionary<string, object>? parameters = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        _transformationRules.Add(new CreateTransformationRuleDto(type, parameters));
        return this;
    }

    internal CreateFieldDto Build() =>
        new(
            Name: _name,
            Type: _type,
            IsRequired: _required,
            ValidationRules: _validationRules.Count != 0 ? _validationRules : null,
            TransformationRules: _transformationRules.Count != 0 ? _transformationRules : null
        );
}
