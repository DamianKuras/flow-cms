using Domain.Common;

namespace Domain.Fields.Validations;

/// <summary>
/// Validates that a text meets a maximum length requirement.
/// </summary>
/// <remarks>
/// This rule ensures that string values do not have more than the specified number of characters.
/// Whitespace characters are counted as valid characters.
/// </remarks>
public class MaximumLengthValidationRule(int MaximumLength) : ValidationRule
{
    public override Capability RequiredCapability =>
        new(Capability.Standard.TEXT);

    /// <summary>
    /// Validates that the provided value is a string not exceeding the maximum length requirement.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>
    /// A <see cref="Result"/> containing a success state if the value is a string with length less
    /// than or equal to <see cref="MaximumLength"/>,
    /// or a failure state with an <see cref="Error"/> if the value is not a string or is greater than <see cref="MaximumLength"/>.
    /// </returns>
    public override Result Validate(object value)
    {
        // var validation_failures = new List<ValidationFailure>();
        if (value is not string str)
        {
            return Result.Failure(Error.Validation("Value is not a string"));
        }

        if (str.Length > MaximumLength)
        {
            return Result.Failure(
                Error.Validation($"Maximum length is {MaximumLength}.")
            );
        }

        return Result.Success();
    }
};
