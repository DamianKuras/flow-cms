using Domain.Common;

namespace Domain.Fields.Transformers;

/// <summary>
/// A transformation rule that converts all characters in a text value to uppercase.
/// </summary>
public class UppercaseTransformer : ITransformationRule
{
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public UppercaseTransformer() { }

    /// <inheritdoc/>
    public string Type => "UppercaseTransformationRule";

    public Capability RequiredCapability => new Capability(Capability.Standard.TEXT);

    /// <inheritdoc/>
    public object? ApplyTransformation(object? value) =>
        value is string s ? s.ToUpperInvariant() : value;
}
