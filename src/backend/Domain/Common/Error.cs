namespace Domain.Common;

/// <summary>
/// Represents the categories of errors that can occur during operations.
/// </summary>
public enum ErrorTypes
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Unauthorized,
    Infrastructure,
}

/// <summary>
/// Represents an error with contextual info message.
/// </summary>
/// <param name="Message">The error message describing what went wrong.</param>
/// <param name="Type">The category of error that occurred.</param>
/// <param name="ValidationFailures">Optional collection of validation failures for Validation error types.</param>
public record Error(
    string Message,
    ErrorTypes Type,
    IReadOnlyList<ValidationResult>? ValidationFailures = null
)
{
    /// <summary>
    /// Creates a NotFound error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An Error with type NotFound.</returns>
    public static Error NotFound(string message) =>
        new(message, ErrorTypes.NotFound);

    /// <summary>
    /// Creates a Forbidden error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An Error with type Forbidden.</returns>
    public static Error Forbidden(string message) =>
        new(message, ErrorTypes.Forbidden);

    /// <summary>
    /// Creates a Conflict error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An Error with type Conflict.</returns>
    public static Error Conflict(string message) =>
        new(message, ErrorTypes.Conflict);

    /// <summary>
    /// Creates a Unauthorized error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An Error with type Unauthorized.</returns>
    public static Error Unauthorized(string message) =>
        new(message, ErrorTypes.Unauthorized);

    /// <summary>
    /// Creates a Infrastructure error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An Error with type Infrastructure.</returns>
    public static Error Infrastructure(string message) =>
        new(message, ErrorTypes.Infrastructure);

    /// <summary>
    /// Creates a Validation error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An Error with type Validation.</returns>
    public static Error Validation(string message) =>
        new(message, ErrorTypes.Validation);

    /// <summary>
    /// Creates a Validation error with field-level failures.
    /// </summary>
    /// <param name="failures">The collection of validation failures.</param>
    /// <returns>An Error with type Validation.</returns>
    public static Error Validation(IReadOnlyList<ValidationResult> failures) =>
        new("Validation failed.", ErrorTypes.Validation, failures);

    /// <summary>
    /// Creates a Validation error with a custom message and field-level failures.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="failures">The collection of validation failures.</param>
    /// <returns>An Error with type Validation.</returns>
    public static Error Validation(
        string message,
        IReadOnlyList<ValidationResult> failures
    ) => new(message, ErrorTypes.Validation, failures);
}
