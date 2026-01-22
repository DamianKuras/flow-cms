using Domain.Common;

namespace Domain.Fields.Transformers;

/// <summary>
/// Defines a contract for transformation rules that modify field values.
/// Transformations are applied to values before validation or storage.
/// </summary>
public interface ITransformationRule
{
    /// <summary>
    /// Gets the type identifier for this transformation rule.
    /// Used to distinguish between different transformation rule implementations.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Gets the capability required to use this transformation rule.
    /// </summary>
    Capability RequiredCapability { get; }

    /// <summary>
    /// Applies the transformation to the provided value.
    /// </summary>
    /// <param name="value">The value to transform. May be null.</param>
    /// <returns>The transformed value, or null if the transformation results in null.</returns>
    object? ApplyTransformation(object? value);
}

/// <summary>
/// Provides a base implementation for transformation rules that require parameters.
/// </summary>
public abstract class ParameterizedTransformationRuleBase : ITransformationRule
{
    /// <summary>
    /// Gets the parameters that configure this transformation rule's behavior.
    /// Keys are parameter names, values are parameter values.
    /// </summary>
    public Dictionary<string, object> Parameters { get; } = new();

    /// <inheritdoc/>
    public abstract Capability RequiredCapability { get; }

    /// <inheritdoc/>
    public abstract string Type { get; }

    /// <inheritdoc/>
    public abstract object? ApplyTransformation(object? value);
}
