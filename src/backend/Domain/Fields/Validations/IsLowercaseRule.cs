using System.Linq;
using Domain.Common;

namespace Domain.Fields.Validations;

/// <summary>
/// Validates that a text value contains only lowercase characters.
/// </summary>
/// <remarks>
/// This rule checks that all alphabetic characters in the string are lowercase.
/// Non-alphabetic characters (numbers, symbols, whitespace) are ignored.
/// Empty strings pass validation.
/// </remarks>
public class IsLowercaseRule : ValidationRule
{
    public override Capability RequiredCapability =>
        new(Capability.Standard.TEXT);

    /// <summary>
    ///  Validates that the provided value is a string containing only lowercase characters.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>
    /// A <see cref="Result"/> containing a success state if the value is a non-empty string with no uppercase characters,
    /// or a failure state with an <see cref="Error"/> if the value is not a string or contains uppercase characters.
    /// </returns>
    public override Result Validate(object? value)
    {
        if (value is not string str)
        {
            return Result.Failure(Error.Validation("Value is not a string"));
        }

        if (str.Any(char.IsUpper))
        {
            return Result.Failure(
                Error.Validation($"String {value} is not lowercase")
            );
        }

        return Result.Success();
    }
};
