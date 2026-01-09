using Application.Auth;
using Domain.Common;
using Domain.Permissions;

namespace Application.Interfaces;

/// <summary>
/// Provides authorization services for validating user permissions against CMS resources and actions.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Validates whether the current user is authorized to perform the specialized action on the specialized resource.
    /// </summary>
    /// <param name="action">The CMS action <see cref="CmsAction"/> to be performed on the specified resource.</param>
    /// <param name="resource">The cms resource <see cref="Resource"/> this action applies to. </param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains <c>true</c> if the action is authorized; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> IsAllowedAsync(CmsAction action, Resource resource, CancellationToken ct);
}
