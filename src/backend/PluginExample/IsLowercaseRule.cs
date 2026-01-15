using Domain.Common;
using Domain.Fields.Validations;

namespace PluginExample;

/// <summary>
/// Validates that a text value contains only lowercase characters.
/// </summary>
/// <remarks>
/// This rule checks that all alphabetic characters in the string are lowercase.
/// Non-alphabetic characters (numbers, symbols, whitespace) are ignored.
/// Empty strings pass validation.
/// </remarks>
public class IsLowercaseRule : IValidationRule
{
    /// <summary>
    /// Gets the unique identifier for this validation rule type.
    /// </summary>
    public string Type => "IsLowercaseRule";

    private const string NotStringError = "Value must be a string.";
    private const string HasUppercaseError = "String contains uppercase characters.";

    /// <summary>
    /// Gets the capability required to use this validation rule.
    /// </summary>
    public Capability RequiredCapability => new(Capability.Standard.TEXT);

    /// <summary>
    ///  Validates that the provided value is a string containing only lowercase characters.
    /// </summary>
    /// <param name="value">The value to validate. Can be null.</param>
    /// <returns>
    /// A <see cref="Result"/> containing a success state if the value is a non-empty string with no uppercase characters,
    /// or a failure state with an <see cref="Error"/> if the value is not a string or contains uppercase characters.
    /// </returns>
    public Result Validate(object? value)
    {
        if (value is not string str)
        {
            return Result.Failure(Error.Validation(NotStringError));
        }

        if (string.IsNullOrEmpty(str))
        {
            return Result.Success();
        }

        if (str.Any(char.IsUpper))
        {
            return Result.Failure(Error.Validation(HasUppercaseError));
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates the value and adds errors to the provided error sink if validation fails.
    /// </summary>
    /// <param name="value">The value to validate. Can be null.</param>
    /// <param name="errors">The error sink to collect validation errors.</param>
    public void Validate(object value, IValidationErrorSink errors)
    {
        if (value is not string str)
        {
            errors.Add(NotStringError);
            return;
        }

        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        if (str.Any(char.IsUpper))
        {
            errors.Add(HasUppercaseError);
        }
    }
};
