using Domain;
using Domain.Common;

namespace Application.Interfaces;

/// <summary>
/// Defines a handler for processing a query and returning a typed response.
/// </summary>
/// <typeparam name="TQuery">The query type with parameters used to execute the query.</typeparam>
/// <typeparam name="TResponse"> The response type returned after processing the query.</typeparam>
public interface IQueryHandler<TQuery, TResponse>
{
    /// <summary>
    /// Handles the specified query and returns a result containing the response data.
    /// </summary>
    /// <param name="query">The query object containing the parameters needed to execute the query operation.</param>
    /// <param name="cancellationToken">A token that propagates notification that operations should be canceled.</param>
    /// <returns>
    /// A <see cref="Task{Result}"/> representing the asynchronous operation.
    /// The result contains the response of type <typeparamref name="TResponse"/>.
    /// </returns>
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
