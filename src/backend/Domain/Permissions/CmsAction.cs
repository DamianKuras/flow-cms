namespace Domain.Permissions;

/// <summary>
/// Defines built-in wildcard actions for permission system.
/// These actions provide special matching behavior across the permission framework.
/// </summary>
public static class BuiltInActions
{
    /// <summary>
    /// Wildcard action that matches all actions.
    /// Use this to grant or check permissions for any action on a resource.
    /// </summary>
    /// <example>
    /// Permission with action "*" will match Read, Create, Update, Delete, etc.
    /// </example>
    public static readonly string All = "*";
}

/// <summary>
/// Defines the available actions that can be performed on CMS resources.
/// These actions represent the standard CRUD operations plus content lifecycle management.
/// </summary>
public enum CmsAction
{
    /// <summary>
    /// Permission to view or retrieve content.
    /// This is typically the most permissive action.
    /// </summary>
    Read = 0,

    /// <summary>
    /// Permission to create new content items.
    /// User can add new resources to the system.
    /// </summary>
    Create = 1,

    /// <summary>
    /// Permission to modify existing content.
    /// User can edit and save changes to resources.
    /// </summary>
    Update = 2,

    /// <summary>
    /// Permission to remove content from the system.
    /// This is typically a destructive operation requiring elevated privileges.
    /// </summary>
    Delete = 3,

    /// <summary>
    /// Permission to publish content, making it publicly visible.
    /// This typically moves content from draft to published state.
    /// </summary>
    Publish = 4,

    /// <summary>
    /// Permission to archive content, removing it from active use.
    /// Archived content is typically hidden but not permanently deleted.
    /// </summary>
    Archive = 5,

    /// <summary>
    /// Permission to list
    /// </summary>
    List = 6,
}
