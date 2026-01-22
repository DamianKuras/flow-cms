using Domain.Common;

namespace Domain.Fields.Validations;

/// <summary>
/// Defines a contract for collecting validation errors during field validation.
/// </summary>
public interface IValidationErrorSink
{
    /// <summary>
    /// Adds a validation error message to the sink.
    /// </summary>
    /// <param name="error">The error message to add.</param>
    void Add(string error);
}

/// <summary>
/// Provides an implementation of <see cref="IValidationErrorSink"/> that collects errors into a <see cref="ValidationResult"/>.
/// </summary>
/// <param name="result">The validation result that will receive the errors. Cannot be null.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
public sealed class ValidationErrorSink(ValidationResult result) : IValidationErrorSink
{
    private readonly ValidationResult _result =
        result ?? throw new ArgumentNullException(nameof(result));

    /// <summary>
    /// Adds a validation error to the underlying validation result.
    /// </summary>
    /// <param name="error">The error message to add.</param>
    public void Add(string error) => _result.AddError(error);
}

/// <summary>
/// Defines a contract for validation rules that can be applied to field values.
/// </summary>
public interface IValidationRule
{
    /// <summary>
    /// Gets the type identifier for this validation rule.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Gets the capability required to use this validation rule.
    /// </summary>
    Capability RequiredCapability { get; }

    /// <summary>
    /// Validates the provided value and reports any errors to the error sink.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="errors">The sink where validation errors should be reported.</param>
    void Validate(object value, IValidationErrorSink errors);
};

/// <summary>
/// Provides a base implementation for validation rules that require parameters.
/// Parameters are stored as key-value pairs and can be used to configure rule behavior.
/// </summary>
public abstract class ParameterizedValidationRuleBase : IValidationRule
{
    /// <summary>
    /// Gets the parameters that configure this validation rule's behavior.
    /// Keys are parameter names, values are parameter values.
    /// </summary>
    public Dictionary<string, object> Parameters { get; } = new();

    /// <inheritdoc/>
    public abstract string Type { get; }

    /// <inheritdoc/>
    public abstract Capability RequiredCapability { get; }

    /// <inheritdoc/>
    public abstract void Validate(object value, IValidationErrorSink errors);
}
