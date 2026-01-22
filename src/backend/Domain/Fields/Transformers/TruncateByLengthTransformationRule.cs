using Domain.Common;

namespace Domain.Fields.Transformers;

/// <summary>
/// Truncates string values to a maximum length.
/// Non-string values are returned unchanged.
/// </summary>
public sealed class TruncateByLengthTransformationRule : ParameterizedTransformationRuleBase
{
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public TruncateByLengthTransformationRule() { }

    /// <summary>
    /// The type identifier for this transformation rule.
    /// </summary>
    public const string TYPE_NAME = "TruncateByLength";

    /// <summary>
    /// The parameter key for the truncation length.
    /// </summary>
    private const string TRUNCATION_LENGTH_PARAM = "truncationLength";

    /// <inheritdoc/>
    public override string Type => TYPE_NAME;

    /// <summary>
    /// The required capability for the truncation rule.
    /// </summary>
    public override Capability RequiredCapability => new Capability(Capability.Standard.TEXT);

    private readonly int _maxLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="TruncateByLengthTransformationRule"/> class
    /// with the specified maximum length.
    /// </summary>
    /// <param name="truncationLength">The maximum length of the string after truncation. Must be non-negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="truncationLength"/> is negative.</exception>
    public TruncateByLengthTransformationRule(int truncationLength)
    {
        if (truncationLength < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(truncationLength),
                truncationLength,
                "Truncation length must be non-negative."
            );
        }

        Parameters.Add(TRUNCATION_LENGTH_PARAM, truncationLength);
        _maxLength = truncationLength;
    }

    /// <inheritdoc/>
    public override object? ApplyTransformation(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is not string str)
        {
            return value; // only transform strings
        }

        if (_maxLength == 0)
        {
            return string.Empty;
        }

        if (str.Length <= _maxLength)
        {
            return str;
        }

        return str[.._maxLength];
    }
}
