namespace Domain.Common;

/// <summary>
/// Represents the outcome of an operation that may succeed or fail.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error>? Errors { get; }

    private Result(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    private Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result Failure(params Error[] errors)
    {
        if (errors is null || errors.Length == 0)
            throw new ArgumentException(
                "At least one error must be provided for failure."
            );

        return new Result(false, errors);
    }

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result Failure(IEnumerable<Error> errors) =>
        Failure(errors.ToArray());
}

/// <summary>
/// Represents the outcome of an operation that may succeed or fail, with a return value.
/// </summary>
/// <typeparam name="T"> The type of the return value when successful. </typeparam>
/// <remarks>
/// When IsSuccess is true, Value contains the result and Errors is null.
/// When IsSuccess is false, Errors contains failure information and Value is null.
/// </remarks>
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public IReadOnlyList<Error>? Errors { get; }

    private Result(bool isSuccess, T value)
    {
        IsSuccess = isSuccess;
        Value = value;
    }

    private Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static Result<T> Success(T value)
    {
        return new(true, value);
    }

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result<T> Failure(params Error[] errors)
    {
        if (errors is null || errors.Length == 0)
            throw new ArgumentException(
                "At least one error must be provided for failure."
            );

        return new(false, errors);
    }

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result<T> Failure(IEnumerable<Error> errors) =>
        Failure(errors.ToArray());

    /// <summary>
    /// Pattern matching helper that returns the result of one of two functions.
    /// </summary>
    /// <typeparam name="TResult">The return type of the match operation.</typeparam>
    /// <param name="onSuccess">Function to execute if the result is successful. Receives the success value.</param>
    /// <param name="onFailure">Function to execute if the result failed. Receives the collection of errors.</param>
    /// <returns>The result of either onSuccess or onFailure, depending on the result state.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either onSuccess or onFailure is null.</exception>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<IReadOnlyList<Error>, TResult> onFailure
    )
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess(Value!) : onFailure(Errors!);
    }
}

/// <summary>
/// Represents validation result for single field.
/// </summary>
/// <param name="FieldName">Field name the validation result refers to.</param>
public sealed class ValidationResult(string FieldName)
{
    public string FieldName { get; } = FieldName;
    public List<string> ValidationErrors { get; } = [];
    public bool IsValid => ValidationErrors.Count == 0;

    /// <summary>
    /// Adds a validation error message for this field.
    /// </summary>
    /// <param name="error">The error message to add.</param>
    public void AddError(string error) => ValidationErrors.Add(error);

    /// <summary>
    /// Adds multiple validation error messages for this field.
    /// </summary>
    /// <param name="errors"> The collection of error messages to add.</param>
    public void AddErrors(IEnumerable<string> errors) =>
        ValidationErrors.AddRange(errors);
}
