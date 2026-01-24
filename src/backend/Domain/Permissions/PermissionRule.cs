namespace Domain.Permissions;

/// <summary>
/// Represents a permission rule that defines whether an actor type can perform
/// a specific action on a resource.
/// </summary>
/// <param name="ActorType">The type of actor this rule applies to.</param>
/// <param name="Action">The CMS action this rule governs.</param>
/// <param name="Resource">The specific resource this rule applies to. Null if this is a type-level rule.</param>
/// <param name="ResourceType">The resource type this rule applies to. Used for type-level permissions when Resource is null.</param>
/// <param name="Scope">
/// The permission scope indicating whether the action is allowed or denied.
/// Defaults to Deny.
/// </param>
/// <remarks>
/// Either Resource or ResourceType must be specified, but not both.
/// - When Resource is specified: Rule applies to that specific resource instance
/// - When ResourceType is specified: Rule applies to all resources of that type (type-level permission)
/// </remarks>
public sealed record PermissionRule(
    ActorType ActorType,
    CmsAction Action,
    Resource? Resource,
    ResourceType? ResourceType,
    PermissionScope Scope = PermissionScope.Deny
)
{
    /// <summary>
    /// Creates a permission rule for a specific resource instance.
    /// </summary>
    /// <param name="actorType">The type of actor this rule applies to.</param>
    /// <param name="action">The CMS action being permitted or denied.</param>
    /// <param name="resource">The specific resource instance this rule applies to.</param>
    /// <param name="scope">The permission scope. Defaults to Allow for convenience in resource-specific grants.</param>
    /// <returns>A new <see cref="PermissionRule"/> configured for a specific resource.</returns>
    public static PermissionRule ForResource(
        ActorType actorType,
        CmsAction action,
        Resource resource,
        PermissionScope scope = PermissionScope.Allow
    ) => new(actorType, action, resource, null, scope);

    /// <summary>
    /// Creates a permission rule for all resources of a specific type (type-level permission).
    /// </summary>
    /// <param name="actorType">The type of actor this rule applies to.</param>
    /// <param name="action">The CMS action being permitted or denied.</param>
    /// <param name="resourceType">The resource type this rule applies to.</param>
    /// <param name="scope">The permission scope. Defaults to Allow for convenience in type-level grants.</param>
    /// <returns>A new <see cref="PermissionRule"/> configured for a resource type.</returns>
    public static PermissionRule ForResourceType(
        ActorType actorType,
        CmsAction action,
        ResourceType resourceType,
        PermissionScope scope = PermissionScope.Allow
    ) => new(actorType, action, null, resourceType, scope);
}
