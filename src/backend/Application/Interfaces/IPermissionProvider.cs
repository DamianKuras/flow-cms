using Domain.Permissions;

namespace Application.Interfaces;

/// <summary>
/// Defines a contract for providing permission rules associated with roles.
/// </summary>
public interface IPermissionProvider
{
    /// <summary>
    /// Asynchronously retrieves the permission rules mapped to the specified collection of role identifiers.
    /// </summary>
    /// <param name="roleIds">The unique identifiers of the roles to fetch permissions for.</param>
    /// <param name="ct">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A read-only collection of <see cref="PermissionRule"/> representing the aggregated permissions.</returns>
    Task<IReadOnlyCollection<PermissionRule>> GetPermissionsAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken ct
    );
}
