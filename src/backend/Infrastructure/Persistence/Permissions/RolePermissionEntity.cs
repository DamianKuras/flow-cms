using Domain.Permissions;

namespace Infrastructure.Persistence.Permissions;

/// <summary>
/// Database entity representing a single permission rule assigned to a role.
/// </summary>
public sealed class RolePermissionEntity
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>The role this permission belongs to.</summary>
    public Guid RoleId { get; set; }

    /// <summary>The CMS action this rule governs.</summary>
    public CmsAction Action { get; set; }

    /// <summary>The type of resource this rule targets.</summary>
    public ResourceType ResourceType { get; set; }

    /// <summary>
    /// The specific resource instance this rule targets.
    /// For content types this is the stable name; for other resources it is the GUID string.
    /// Null when the rule applies to all resources of <see cref="ResourceType"/>.
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>Whether the rule allows or denies the action.</summary>
    public PermissionScope Scope { get; set; }
}
