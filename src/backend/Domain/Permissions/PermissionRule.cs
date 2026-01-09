namespace Domain.Permissions;

/// <summary>
/// Represents a permission rule that defines whether an actor type can perform
/// a specific action on a resource.
/// </summary>
/// <param name="ActorType">The type of actor this rule applies to.</param>
/// <param name="Action">The CMS action this rule governs.</param>
/// <param name="Resource">The resource this rule applies to.</param>
/// <param name="Scope">
/// The permission scope indicating whether the action is allowed or denied.
/// Defaults to Allow.
/// </param>
public sealed record PermissionRule(
    ActorType ActorType,
    CmsAction Action,
    Resource Resource,
    PermissionScope Scope = PermissionScope.Deny
);
