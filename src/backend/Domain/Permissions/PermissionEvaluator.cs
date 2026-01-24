using Domain.Users;

namespace Domain.Permissions;

/// <summary>
/// Evaluates whether an actor has permission to perform an action on a resource.
/// </summary>
public interface IPermissionEvaluator
{
    /// <summary>
    /// Determines if the specified actor is allowed to perform the action on the resource.
    /// </summary>
    /// <param name="actor">The actor requesting permission.</param>
    /// <param name="action">The action being requested.</param>
    /// <param name="resource">The resource being accessed.</param>
    /// <param name="rules">The set of permission rules to evaluate.</param>
    /// <returns>True if the action is allowed; otherwise, false.</returns>
    bool IsAllowed(
        IActor actor,
        CmsAction action,
        Resource resource,
        IEnumerable<PermissionRule> rules
    );

    /// <summary>
    /// Determines if the specified actor is allowed to perform the action on all resources of the specified type.
    /// </summary>
    /// <param name="actor">The actor requesting permission.</param>
    /// <param name="action">The action being requested.</param>
    /// <param name="resourceType">The type of resources being accessed.</param>
    /// <param name="rules">The set of permission rules to evaluate.</param>
    /// <returns>True if the action is allowed for all resources of the type; otherwise, false.</returns>
    /// <remarks>
    /// This method checks if the actor has global permissions for the resource type,
    /// typically used for list or index operations.
    /// </remarks>
    bool IsAllowedForAll(
        UserActor actor,
        CmsAction action,
        ResourceType resourceType,
        IReadOnlyCollection<PermissionRule> rules
    );
}

/// <summary>
/// Evaluates whether an actor has permission to perform an action on a resource.
/// </summary>
public sealed class PermissionEvaluator : IPermissionEvaluator
{
    /// <inheritdoc/>
    public bool IsAllowed(
        IActor actor,
        CmsAction action,
        Resource resource,
        IEnumerable<PermissionRule> rules
    )
    {
        IEnumerable<PermissionRule> relevant = rules
            .Where(r => r.ActorType == actor.Type && r.Action == action && r.Resource == resource)
            .ToList();

        if (relevant.Any(r => r.Scope == PermissionScope.Deny))
        {
            return false;
        }

        return relevant.Any(r => r.Scope == PermissionScope.Allow);
    }

    /// <inheritdoc/>
    public bool IsAllowedForAll(
        UserActor actor,
        CmsAction action,
        ResourceType resourceType,
        IReadOnlyCollection<PermissionRule> rules
    )
    {
        IEnumerable<PermissionRule> relevant = rules
            .Where(r =>
                r.ActorType == actor.Type && r.Action == action && r.ResourceType == resourceType
            )
            .ToList();

        if (relevant.Any(r => r.Scope == PermissionScope.Deny))
        {
            return false;
        }

        return relevant.Any(r => r.Scope == PermissionScope.Allow);
    }
}
