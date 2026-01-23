using Domain;
using Domain.Common;

namespace Application.Interfaces;

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
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
