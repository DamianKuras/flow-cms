using Domain.Common;

namespace Domain.Fields.Validations;

/// <summary>
/// Validates that a text meets a maximum length requirement.
/// </summary>
/// <remarks>
/// This rule ensures that string values do not have more than the specified number of characters.
/// Whitespace characters are counted as valid characters.
/// </remarks>
public class MaximumLengthValidationRule : ParameterizedValidationRuleBase
{
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public MaximumLengthValidationRule() { }

    private const string MAX_LENGTH_PARAMETER_KEY = "max-length";

    /// <summary>
    /// Constructor for Maximum length rule.
    /// </summary>
    /// <param name="maximumLength">The value for the maximum length.</param>
    public MaximumLengthValidationRule(int maximumLength) =>
        Parameters.Add(MAX_LENGTH_PARAMETER_KEY, maximumLength);

    /// <inheritdoc/>
    public override Capability RequiredCapability => new(Capability.Standard.TEXT);

    /// <inheritdoc/>
    public override string Type => TYPE_NAME;

    /// <summary>
    /// Validates that the provided value is a string not exceeding the maximum length requirement.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="errors">The sink object for adding errors.</param>
    public override void Validate(object value, IValidationErrorSink errors)
    {
        if (value is not string str)
        {
            errors.Add("Value must be string.");
            return;
        }
        if (!int.TryParse(Parameters[MAX_LENGTH_PARAMETER_KEY]?.ToString(), out int maxLength))
        {
            errors.Add("Invalid maximum length configuration.");
            return;
        }

        if (str.Length > maxLength)
        {
            errors.Add($"Maximum length is {maxLength}.");
        }
    }

    /// <summary>
    /// Type name of the maximum length validation rule.
    /// </summary>
    public const string TYPE_NAME = "MaximumLengthValidationRule";
};
