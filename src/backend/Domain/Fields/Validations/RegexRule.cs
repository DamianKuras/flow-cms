using System.Text.RegularExpressions;
using Domain.Common;

namespace Domain.Fields.Validations;

/// <summary>
/// Validates that a text value matches a specified regular expression pattern.
/// </summary>
/// <param name="pattern">
/// The regular expression pattern to match against.
/// Must be a valid .NET regex pattern.
/// </param>
/// <exception cref="ArgumentException"> Thrown when <paramref name="pattern"/>
/// is not a valid regex.</exception>
/// <exception cref="ArgumentNullException"> Thrown when <paramref name="pattern"/>
/// is null.</exception>
/// <exception cref="RegexMatchTimeoutException"> Thrown when the execution time of a regular expression
///  pattern-matching method exceeds its time-out interval.</exception>
public class RegexRule(string pattern) : ValidationRule
{
    public string Pattern { get; } = pattern;

    public override Capability RequiredCapability =>
        new(Capability.Standard.TEXT);

    public override Result Validate(object value)
    {
        if (value is not string s)
            return Result.Failure(Error.Validation("Value must be string."));

        return Regex.IsMatch(s, Pattern)
            ? Result.Success()
            : Result.Failure(
                Error.Validation($"Value does not match pattern '{Pattern}'.")
            );
    }
}
