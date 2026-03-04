using Domain.Permissions;

namespace Application.Interfaces;

/// <summary>
/// Defines a contract for managing the permissions associated with system roles.
/// </summary>
public interface IRolePermissionService
{
    /// <summary>
    /// Asynchronously adds a specific permission rule to a given role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role.</param>
    /// <param name="rule">The permission rule defining the action, resource, and scope to grant.</param>
    /// <returns>A task that represents the asynchronous addition process.</returns>
    Task AddPermissionToRoleAsync(Guid roleId, PermissionRule rule);

    /// <summary>
    /// Asynchronously removes a specific permission rule from a given role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role.</param>
    /// <param name="rule">The permission rule to revoke.</param>
    /// <returns>A task that represents the asynchronous removal process.</returns>
    Task RemovePermissionFromRoleAsync(Guid roleId, PermissionRule rule);
}
