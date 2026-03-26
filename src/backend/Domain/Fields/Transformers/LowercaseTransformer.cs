using Domain.Common;

namespace Domain.Fields.Transformers;

/// <summary>
/// A transformation rule that converts all characters in a text value to lowercase.
/// </summary>
public class LowercaseTransformer : ITransformationRule
{
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public LowercaseTransformer() { }

    /// <inheritdoc/>
    public string Type => "LowercaseTransformationRule";

    /// <inheritdoc/>
    public Capability RequiredCapability => new(Capability.Standard.TEXT);

    /// <inheritdoc/>
    public object? ApplyTransformation(object? value) =>
        value is string s ? s.ToLowerInvariant() : value;
}
