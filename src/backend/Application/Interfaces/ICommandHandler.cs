using Domain;
using Domain.Common;

namespace Application.Interfaces;

/// <summary>
/// Represents the response from a command operation, encapsulating either a successful result, a failure, or validation errors.
/// </summary>
/// <typeparam name="T">The type of data returned in a successful command execution.</typeparam>
public class CommandResponse<T>
{
    /// <summary>
    /// Gets the result of the command operation, containing either success data or an error.
    /// This property is null when the response contains validation errors.
    /// </summary>
    public Result<T>? Result { get; init; }

    /// <summary>
    /// Gets the validation result containing field-level validation errors.
    /// This property is null when the response contains an operation result.
    /// </summary>
    public MultiFieldValidationResult? Validation { get; init; }

    /// <summary>
    /// Creates a successful command response with the specified data.
    /// </summary>
    /// <param name="data">The data to include in the successful response.</param>
    /// <returns>A <see cref="CommandResponse{T}"/> indicating successful execution with the provided data.</returns>
    public static CommandResponse<T> Success(T data) => new() { Result = Result<T>.Success(data) };

    /// <summary>
    /// Creates a failed command response with the specified error.
    /// </summary>
    /// <param name="error">The error that caused the command to fail.</param>
    /// <returns>A <see cref="CommandResponse{T}"/> indicating failure with the provided error.</returns>
    public static CommandResponse<T> Failure(Error error) =>
        new() { Result = Result<T>.Failure(error) };

    /// <summary>
    /// Creates a command response indicating validation failure with field-level errors.
    /// </summary>
    /// <param name="validation">The validation result containing the field-level validation errors.</param>
    /// <returns>A <see cref="CommandResponse{T}"/> containing the validation errors.</returns>
    public static CommandResponse<T> ValidationFailed(MultiFieldValidationResult validation) =>
        new() { Validation = validation };
}

/// <summary>
/// Defines a handler for processing commands and returning typed responses.
/// </summary>
/// <typeparam name="TCommand">The command type with parameters required to execute the operation.</typeparam>
/// <typeparam name="TResponse">The type of response object returned after processing the command.</typeparam>
public interface ICommandHandler<TCommand, TResponse>
{
    /// <summary>
    /// Processes the specified command and returns a result containing the response data.
    /// </summary>
    /// <param name="command">The command object containing the parameters for execution.</param>
    /// <param name="cancellationToken">A token that propagates notification that operations should be canceled.</param>
    /// <returns>
    /// A <see cref="Task{Result}"/> representing the asynchronous operation.
    /// The result contains the response of type <typeparamref name="TResponse"/>.
    /// </returns>
    Task<CommandResponse<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
