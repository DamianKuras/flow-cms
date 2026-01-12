namespace Domain.Fields.Transformers;

/// <summary>
/// Transformation rule that truncates string values to a specified maximum length.
/// Non-string values are passed through unchanged.
/// </summary>
public sealed class TruncateByLengthTransformationRule : ParameterizedTransformationRuleBase
{
    /// <summary>
    /// The type identifier for this transformation rule.
    /// </summary>
    public const string TYPE_NAME = "TruncateByLength";

    /// <summary>
    /// Gets the type identifier for this transformation rule.
    /// </summary>
    public override string Type => TYPE_NAME;

    /// <summary>
    /// The maximum length to which strings will be truncated.
    /// </summary>
    private readonly int _maxLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="TruncateByLengthTransformationRule"/> class.
    /// This parameterless constructor is intended for deserialization scenarios.
    /// </summary>
    public TruncateByLengthTransformationRule() => _maxLength = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="TruncateByLengthTransformationRule"/> class
    /// with the specified truncation length.
    /// </summary>
    /// <param name="truncationLength">The maximum length for truncated strings. Must be non-negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="truncationLength"/> is negative.
    /// </exception>
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

        Parameters.Add("truncationLength", truncationLength);
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
            return value;
        }

        int maxLength = _maxLength;
        if (maxLength == 0 && Parameters.TryGetValue("truncationLength", out object? paramValue))
        {
            maxLength = Convert.ToInt32(paramValue);
        }

        if (maxLength == 0)
        {
            return string.Empty;
        }

        if (str.Length <= maxLength)
        {
            return str;
        }

        return str[..maxLength];
    }
}
