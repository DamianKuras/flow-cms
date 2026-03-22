namespace Domain.Permissions;

/// <summary>
/// Represents a permission rule that defines whether an actor type can perform
/// a specific action on a resource. Use <see cref="ForResource"/> or
/// <see cref="ForResourceType"/> to construct instances.
/// </summary>
public sealed record PermissionRule
{
    /// <summary>The type of actor this rule applies to.</summary>
    public ActorType ActorType { get; }

    /// <summary>The CMS action this rule governs.</summary>
    public CmsAction Action { get; }

    /// <summary>The specific resource this rule applies to.</summary>
    public Resource? Resource { get; }

    /// <summary>The resource type this rule applies to.</summary>
    public ResourceType? ResourceType { get; }

    /// <summary>Whether the action is allowed or denied.</summary>
    public PermissionScope Scope { get; }

    private PermissionRule(
        ActorType actorType,
        CmsAction action,
        Resource? resource,
        ResourceType? resourceType,
        PermissionScope scope
    )
    {
        if (resource is not null && resourceType is not null)
        {
            throw new ArgumentException("Specify either Resource or ResourceType, not both.");
        }

        if (resource is null && resourceType is null)
        {
            throw new ArgumentException("Either Resource or ResourceType must be specified.");
        }

        ActorType = actorType;
        Action = action;
        Resource = resource;
        ResourceType = resourceType;
        Scope = scope;
    }

    /// <summary>Creates a permission rule for a specific resource instance.</summary>
    public static PermissionRule ForResource(
        ActorType actorType,
        CmsAction action,
        Resource resource,
        PermissionScope scope = PermissionScope.Allow
    ) => new(actorType, action, resource, null, scope);

    /// <summary>Creates a type-level permission rule that applies to all resources of a given type.</summary>
    public static PermissionRule ForResourceType(
        ActorType actorType,
        CmsAction action,
        ResourceType resourceType,
        PermissionScope scope = PermissionScope.Allow
    ) => new(actorType, action, null, resourceType, scope);

    /// <summary>Returns true if this rule targets the given resource instance.</summary>
    public bool AppliesToResource(Resource resource) => Resource == resource;

    /// <summary>Returns true if this rule targets all resources of the given type.</summary>
    public bool AppliesToResourceType(ResourceType type) => ResourceType == type;
}
