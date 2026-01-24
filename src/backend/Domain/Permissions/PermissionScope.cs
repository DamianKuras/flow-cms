namespace Domain.Permissions;

/// <summary>
/// Specifies the scope of a permission, indicating whether access is granted or denied.
/// </summary>
public enum PermissionScope
{
    /// <summary>
    /// Access is explicitly denied. This takes precedence over allow permissions.
    /// </summary>
    Deny,

    /// <summary>
    /// Access is granted. The user or entity has permission to perform the action.
    /// </summary>
    Allow,
}
