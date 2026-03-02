using Domain.Common;

namespace Domain.Fields.Transformers;

/// <summary>
/// A transformation rule that converts text into title case, capitalizing the first letter of each word.
/// </summary>
public class CapitalizeTransformer : ITransformationRule
{
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public CapitalizeTransformer() { }

    /// <inheritdoc/>
    public string Type => "CapitalizeTransformationRules";

    /// <inheritdoc/>
    public Capability RequiredCapability => new Capability(Capability.Standard.TEXT);

    /// <inheritdoc/>
    public object? ApplyTransformation(object? value)
    {
        string? text = value as string;
        return string.IsNullOrEmpty(text)
            ? value
            : System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
    }
}
