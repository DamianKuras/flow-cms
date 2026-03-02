using Domain.Common;

namespace Domain.Fields.Transformers;

/// <summary>
/// A transformation rule that removes all characters from a text value except alphanumeric characters (letters and numbers) and spaces.
/// </summary>
public class RemoveSpecialCharsTransformer : ITransformationRule
{
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public RemoveSpecialCharsTransformer() { }

    /// <inheritdoc/>
    public string Type => "RemoveSpecialCharactersTransformationRules";

    public Capability RequiredCapability => new(Capability.Standard.TEXT);

    /// <inheritdoc/>
    public object? ApplyTransformation(object? value)
    {
        string? text = value as string;
        return string.IsNullOrEmpty(text)
            ? value
            : System.Text.RegularExpressions.Regex.Replace(text, @"[^a-zA-Z0-9\s]", "");
    }
}
