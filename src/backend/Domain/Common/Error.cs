namespace Domain.Common;

/// <summary>
/// Represents the categories of errors that can occur during operations.
/// </summary>
public enum ErrorTypes
{
    /// <summary>
    /// Indicates that the error was caused by invalid input.
    /// </summary>
    Validation,

    /// <summary>
    /// Indicates that a requested resource could not be found.
    /// </summary>
    NotFound,

    /// <summary>
    /// Indicates that the operation could not be completed due to a conflict
    /// with the current state of the resource.
    /// </summary>
    Conflict,

    /// <summary>
    /// Indicates that the operation is not allowed due to insufficient permissions.
    /// </summary>
    Forbidden,

    /// <summary>
    /// Indicates that authentication is required or has failed.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// Indicates that an infrastructure or system-level error has occurred.
    /// </summary>
    Infrastructure,
}

/// <summary>
/// Represents an error with contextual information.
/// </summary>
/// <param name="Message">The error message describing what went wrong.</param>
/// <param name="Type">The category of error that occurred.</param>
public record Error(string Message, ErrorTypes Type)
{
    /// <summary>
    /// Creates a <see cref="ErrorTypes.NotFound"/> error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An <see cref="Error"/> with type <see cref="ErrorTypes.NotFound"/>.</returns>
    public static Error NotFound(string message) => new(message, ErrorTypes.NotFound);

    /// <summary>
    /// Creates a <see cref="ErrorTypes.Forbidden"/> error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An <see cref="Error"/> with type <see cref="ErrorTypes.Forbidden"/>.</returns>
    public static Error Forbidden(string message) => new(message, ErrorTypes.Forbidden);

    /// <summary>
    /// Creates a <see cref="ErrorTypes.Conflict"/> error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An <see cref="Error"/> with type <see cref="ErrorTypes.Conflict"/>.</returns>
    public static Error Conflict(string message) => new(message, ErrorTypes.Conflict);

    /// <summary>
    /// Creates an <see cref="ErrorTypes.Unauthorized"/> error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An <see cref="Error"/> with type <see cref="ErrorTypes.Unauthorized"/>.</returns>
    public static Error Unauthorized(string message) => new(message, ErrorTypes.Unauthorized);

    /// <summary>
    /// Creates an <see cref="ErrorTypes.Infrastructure"/> error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An <see cref="Error"/> with type <see cref="ErrorTypes.Infrastructure"/>.</returns>
    public static Error Infrastructure(string message) => new(message, ErrorTypes.Infrastructure);

    /// <summary>
    /// Creates a <see cref="ErrorTypes.Validation"/> error with a default message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An <see cref="Error"/> with type <see cref="ErrorTypes.Validation"/>.</returns>
    public static Error Validation(string message) => new(message, ErrorTypes.Validation);
}
