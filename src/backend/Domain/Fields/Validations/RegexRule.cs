using System.Text.RegularExpressions;
using Domain.Common;

namespace Domain.Fields.Validations;

/// <summary>
/// Validates that a text value matches a specified regular expression pattern.
/// </summary>
/// <exception cref="ArgumentException"> Thrown when <paramref name="pattern"/>
/// is not a valid regex.</exception>
/// <exception cref="ArgumentNullException"> Thrown when <paramref name="pattern"/>
/// is null.</exception>
/// <exception cref="RegexMatchTimeoutException"> Thrown when the execution time of a regular expression
///  pattern-matching method exceeds its time-out interval.</exception>
public class RegexRule : ParameterizedValidationRuleBase
{
    private const string REGEX_PARAMETER_KEY = "regex";

    /// <summary>
    /// Constructor for Regex rule.
    /// </summary>
    /// <param name="pattern">
    /// The regular expression pattern to match against.
    /// Must be a valid .NET regex pattern.
    /// </param>
    public RegexRule(string pattern) => Parameters.Add(REGEX_PARAMETER_KEY, pattern);

    /// <inheritdoc/>
    public override string Type => "RegexRule";

    /// <inheritdoc/>
    public override Capability RequiredCapability => new(Capability.Standard.TEXT);

    /// <inheritdoc/>
    public override void Validate(object value, IValidationErrorSink errors)
    {
        if (value is not string s)
        {
            errors.Add("Value must be string.");
            return;
        }
        string? pattern = Parameters[REGEX_PARAMETER_KEY]?.ToString();
        if (pattern is null)
        {
            errors.Add("Invalid pattern configuration.");
            return;
        }
        if (!Regex.IsMatch(s, pattern))
        {
            errors.Add($"Value does not match pattern '{pattern}'.");
        }
    }
}
