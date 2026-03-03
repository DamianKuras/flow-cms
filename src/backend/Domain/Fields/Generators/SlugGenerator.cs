namespace Domain.Fields.Generators;

/// <summary>
/// A field generator that parses a string value into a URL-friendly slug.
/// </summary>
public class SlugGenerator : FieldGenerator
{
    /// <inheritdoc/>
    public override object? GenerateValue(object? sourceValue)
    {
        string? text = sourceValue as string;
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        text = text.ToLower().Trim();
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[^\w\s-]", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[\s_]+", "-");
        return System.Text.RegularExpressions.Regex.Replace(text, @"-+", "-").Trim('-');
    }
}
