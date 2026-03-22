using Domain.Permissions;

namespace Domain.Roles;

/// <summary>
/// Represents a role that can be assigned to users, defining a collection of permissions.
/// </summary>
public sealed class Role
{
    private Role() { }

    public Role(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>The unique identifier for the role.</summary>
    public Guid Id { get; }

    /// <summary>The name of the role.</summary>
    public string Name { get; } = "";

    private readonly HashSet<PermissionRule> _permissions = [];

    /// <summary>Gets the permission rules associated with this role.</summary>
    public IReadOnlyCollection<PermissionRule> Permissions => _permissions;

    /// <summary>Adds a permission rule to this role.</summary>
    public void AddPermission(PermissionRule rule) => _permissions.Add(rule);
}
