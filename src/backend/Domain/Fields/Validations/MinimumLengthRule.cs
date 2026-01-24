using Domain.Common;

namespace Domain.Fields.Validations;

/// <summary>
/// Validates that a text value meets a minimum length requirement.
/// </summary>
/// <remarks>
/// This rule ensures that string values have at least the specified number of characters.
/// Whitespace characters are counted as valid characters.
/// </remarks>
public class MinimumLengthValidationRule : ParameterizedValidationRuleBase
{
    private const string MIN_LENGTH_PARAMETER_KEY = "min-length";

    /// <summary>
    /// Empty constructor.
    /// </summary>
    public MinimumLengthValidationRule() { }

    /// <summary>
    /// Constructor for Minimum length rule.
    /// </summary>
    /// <param name="minimumLength">The value for the minimum length.</param>
    public MinimumLengthValidationRule(int minimumLength) =>
        Parameters.Add(MIN_LENGTH_PARAMETER_KEY, minimumLength);

    /// <inheritdoc/>
    public override Capability RequiredCapability => new(Capability.Standard.TEXT);

    /// <inheritdoc/>
    public override string Type => "MinimumLengthValidationRule";

    /// <summary>
    /// Validates that the provided value is a string not shorter than minimum length requirement.
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
        if (!int.TryParse(Parameters[MIN_LENGTH_PARAMETER_KEY]?.ToString(), out int minLength))
        {
            errors.Add("Invalid minimum length configuration.");
            return;
        }
        if (str.Length < minLength)
        {
            errors.Add($"Minimum length is {minLength}.");
        }
    }
};
