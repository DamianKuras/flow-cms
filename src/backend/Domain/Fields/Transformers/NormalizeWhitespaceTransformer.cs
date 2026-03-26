using Domain.Common;

namespace Domain.Fields.Transformers;

/// <summary>
/// A transformation rule that reduces multiple consecutive whitespace characters to a single space and trims leading/trailing whitespace.
/// </summary>
public class NormalizeWhitespaceTransformer : ITransformationRule
{
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public NormalizeWhitespaceTransformer() { }

    /// <inheritdoc/>
    public string Type => "NormalizeWhitespaceTransformationRules";

    /// <inheritdoc/>
    public Capability RequiredCapability => new(Capability.Standard.TEXT);

    /// <inheritdoc/>
    public object? ApplyTransformation(object? value)
    {
        string? text = value as string;
        return string.IsNullOrEmpty(text)
            ? value
            : System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
    }
}
