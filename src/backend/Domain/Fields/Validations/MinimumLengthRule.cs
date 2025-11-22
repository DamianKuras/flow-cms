using Domain.Common;

namespace Domain.Fields.Validations;

/// <summary>
/// Validates that a text value meets a minimum length requirement.
/// </summary>
/// <remarks>
/// This rule ensures that string values have at least the specified number of characters.
/// Whitespace characters are counted as valid characters.
/// </remarks>
public class MinimumLengthValidationRule(int MinimumLength) : ValidationRule
{
    public override Capability RequiredCapability =>
        new(Capability.Standard.TEXT);

    /// <summary>
    /// Validates that the provided values is a string meeting the minimum length requirement.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>
    /// A <see cref="Result"/> containing a success state if the value is a string with length greater
    /// than or equal to <see cref="MinimumLength"/>,
    /// or a failure state with an <see cref="Error"/> if the value is not a string or is shorter than <see cref="MinimumLength"/>.
    /// </returns>
    public override Result Validate(object value)
    {
        if (value is not string str)
        {
            return Result.Failure(Error.Validation("Value is not a string"));
        }

        if (str.Length < MinimumLength)
        {
            return Result.Failure(
                Error.Validation($"Minimum length is {MinimumLength}.")
            );
        }

        return Result.Success();
    }
};
