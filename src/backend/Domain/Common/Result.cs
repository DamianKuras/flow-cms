namespace Domain.Common;

/// <summary>
/// Represents the kind of failure returned by operations.
/// </summary>
public enum FailureKind
{
    /// <summary>
    /// No failure, the operation succeeded.
    /// </summary>
    None,

    /// <summary>
    /// A domain level business error.
    /// </summary>
    DomainError,

    /// <summary>
    /// A single field failed validation.
    /// </summary>
    FieldValidation,

    /// <summary>
    /// Multiple fields failed validation.
    /// </summary>
    MultiFieldValidation,
}

/// <summary>
/// Represents the outcome of an operation that may succeed or fail.
/// </summary>
/// <remarks>
/// Use this for operations that don't return a value. For operations that return a value, use <see cref="Result{T}"/>.
/// </remarks>
public class Result
{
    /// <summary>
    /// Gets the kind of failure that occurred, or <see cref="FailureKind.None"/> if the operation succeeded.
    /// </summary>
    public FailureKind FailureKind { get; } = FailureKind.None;

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess => FailureKind == FailureKind.None;

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the domain error that caused the failure, or null if no domain error occurred.
    /// </summary>
    public Error? Error { get; } = null;

    /// <summary>
    /// Gets the multi-field validation result if multiple fields failed validation, or null otherwise.
    /// </summary>
    public MultiFieldValidationResult? MultiFieldValidationResult { get; }

    /// <summary>
    /// Gets the single-field validation result if a field failed validation, or null otherwise.
    /// </summary>
    public ValidationResult? ValidationResult { get; }

    private Result() { }

    private Result(Error error)
    {
        FailureKind = FailureKind.DomainError;
        Error = error;
    }

    private Result(ValidationResult v)
    {
        FailureKind = FailureKind.FieldValidation;
        ValidationResult = v;
    }

    private Result(MultiFieldValidationResult mv)
    {
        FailureKind = FailureKind.MultiFieldValidation;
        MultiFieldValidationResult = mv;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A <see cref="Result"/> representing a successful operation.</returns>>
    public static Result Success() => new();

    /// <summary>
    /// Creates a failed result with the specified domain error.
    /// </summary>
    /// <param name="error">The domain error that caused the failure.</param>
    /// <returns>A <see cref="Result"/> representing a failed operation with a domain error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public static Result Failure(Error error)
    {
        if (error is null)
        {
            throw new ArgumentException("Error must be provided for failure.");
        }

        return new(error);
    }

    /// <summary>
    /// Creates a failed result with multi-field validation errors.
    /// </summary>
    /// <param name="vr">The multi-field validation result containing validation errors.</param>
    /// <returns>A <see cref="Result"/> representing a failed operation with multi-field validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="vr"/> is null.</exception>
    public static Result MultiFieldValidationFailure(MultiFieldValidationResult vr) => new(vr);

    /// <summary>
    /// Creates a failed result with a single-field validation error.
    /// </summary>
    /// <param name="v">The validation result for a single field.</param>
    /// <returns>A <see cref="Result"/> representing a failed operation with a single-field validation error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="v"/> is null.</exception>
    public static Result FieldValidationFailure(ValidationResult v) => new(v);
}

/// <summary>
/// Represents the outcome of an operation that may succeed or fail, with a return value.
/// </summary>
/// <typeparam name="T">The type of the return value when successful.</typeparam>
/// <remarks>
/// When <see cref="IsSuccess"/> is true, <see cref="Value"/> contains the result and error properties are null.
/// When <see cref="IsSuccess"/> is false, error properties contain failure information and <see cref="Value"/> is null.
/// </remarks>
public class Result<T>
{
    /// <summary>
    /// Gets the kind of failure that occurred, or <see cref="FailureKind.None"/> if the operation succeeded.
    /// </summary>
    public FailureKind FailureKind { get; } = FailureKind.None;

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess => FailureKind == FailureKind.None;

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value returned by the successful operation, or null if the operation failed.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the domain error that caused the failure, or null if no domain error occurred.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Gets the multi-field validation result if multiple fields failed validation, or null otherwise.
    /// </summary>
    public MultiFieldValidationResult? MultiFieldValidationResult { get; }

    /// <summary>
    /// Gets the single-field validation result if a field failed validation, or null otherwise.
    /// </summary>
    public ValidationResult? ValidationResult { get; }

    private Result(T value) => Value = value;

    private Result(Error error)
    {
        FailureKind = FailureKind.DomainError;
        Error = error;
    }

    private Result(MultiFieldValidationResult multiFieldValidationResult)
    {
        FailureKind = FailureKind.MultiFieldValidation;

        MultiFieldValidationResult = multiFieldValidationResult;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class with a single-field validation result.
    /// </summary>
    /// <param name="validationResult">The validation result for a single field.</param>
    private Result(ValidationResult validationResult)
    {
        FailureKind = FailureKind.FieldValidation;
        ValidationResult = validationResult;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The value returned by the successful operation.</param>
    /// <returns>A <see cref="Result{T}"/> representing a successful operation with a value.</returns>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified domain error.
    /// </summary>
    /// <param name="error">The domain error that caused the failure.</param>
    /// <returns>A <see cref="Result{T}"/> representing a failed operation with a domain error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public static Result<T> Failure(Error error)
    {
        if (error is null)
        {
            throw new ArgumentException("Error must be provided for failure.", nameof(error));
        }

        return new Result<T>(error);
    }

    /// <summary>
    /// Creates a failed result with multi-field validation errors.
    /// </summary>
    /// <param name="multiFieldValidationResult">The multi-field validation result containing validation errors.</param>
    /// <returns>A <see cref="Result{T}"/> representing a failed operation with multi-field validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="multiFieldValidationResult"/> is null.</exception>
    public static Result<T> MultiFieldValidationFailure(
        MultiFieldValidationResult multiFieldValidationResult
    )
    {
        ArgumentNullException.ThrowIfNull(multiFieldValidationResult);

        return new Result<T>(multiFieldValidationResult);
    }

    /// <summary>
    /// Creates a failed result with a single-field validation error.
    /// </summary>
    /// <param name="validationResult">The validation result for a single field.</param>
    /// <returns>A <see cref="Result{T}"/> representing a failed operation with a single-field validation error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validationResult"/> is null.</exception>
    public static Result<T> FieldValidationFailure(ValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(validationResult);

        return new Result<T>(validationResult);
    }
}

/// <summary>
/// Represents validation result for a single field.
/// </summary>
/// <param name="fieldName">The name of the field this validation result refers to.</param>
public sealed class ValidationResult(string fieldName)
{
    /// <summary>
    /// Gets the name of the field this validation result refers to.
    /// </summary>
    public string FieldName { get; } = fieldName;

    /// <summary>
    /// Gets the list of validation error messages for this field.
    /// </summary>
    public List<string> ValidationErrors { get; } = [];

    /// <summary>
    /// Gets a value indicating whether this field passed validation (has no errors).
    /// </summary>
    public bool IsValid => ValidationErrors.Count == 0;

    /// <summary>
    /// Gets a value indicating whether this field failed validation (has errors).
    /// </summary>
    public bool IsInvalid => ValidationErrors.Count > 0;

    /// <summary>
    /// Adds a validation error message for this field.
    /// </summary>
    /// <param name="error">The error message to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null or whitespace.</exception>
    public void AddError(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentNullException(
                nameof(error),
                "Error message cannot be null or empty."
            );
        }

        ValidationErrors.Add(error);
    }

    /// <summary>
    /// Adds multiple validation error messages for this field.
    /// </summary>
    /// <param name="errors">The collection of error messages to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    public void AddErrors(IEnumerable<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        ValidationErrors.AddRange(errors.Where(e => !string.IsNullOrWhiteSpace(e)));
    }
}

/// <summary>
/// Represents validation results for multiple fields.
/// </summary>
public sealed class MultiFieldValidationResult()
{
    /// <summary>
    /// Gets the list of validation results for individual fields.
    /// </summary>
    public List<ValidationResult> ValidationResults { get; } = [];

    /// <summary>
    /// Adds a validation result for a field.
    /// </summary>
    /// <param name="result">The validation result to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public void AddValidationResult(ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        ValidationResults.Add(result);
    }

    /// <summary>
    /// Gets a value indicating whether all fields passed validation.
    /// </summary>
    public bool IsSuccess => ValidationResults.All(x => x.IsValid);

    /// <summary>
    /// Gets a value indicating whether any field failed validation.
    /// </summary>
    public bool IsFailure => ValidationResults.Any(x => x.IsInvalid);

    /// <summary>
    /// Gets a validation result for a specific field.
    /// </summary>
    /// <param name="fieldName">The name of the field to retrieve validation results for.</param>
    /// <returns>The <see cref="ValidationResult"/> for the specified field, or null if not found.</returns>
    public ValidationResult? GetFieldResult(string fieldName) =>
        ValidationResults.FirstOrDefault(vr => vr.FieldName == fieldName);

    /// <summary>
    /// Checks if a specific field has a particular error message.
    /// </summary>
    /// <param name="fieldName">The name of the field to check.</param>
    /// <param name="errorMessage">The error message to look for.</param>
    /// <returns>True if the field has the specified error message; otherwise, false.</returns>
    public bool HasFieldError(string fieldName, string errorMessage)
    {
        ValidationResult? fieldResult = GetFieldResult(fieldName);
        return fieldResult?.ValidationErrors.Contains(errorMessage) ?? false;
    }

    /// <summary>
    /// Gets all validation errors from all fields as a flat list.
    /// </summary>
    /// <returns>An enumerable of all error messages across all fields.</returns>
    public IEnumerable<string> GetAllErrors() =>
        ValidationResults.Where(vr => vr.IsInvalid).SelectMany(vr => vr.ValidationErrors);
}
