namespace Domain.Users;

/// <summary>
/// Defines the possible status values for a user account.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// The user account is active and fully functional.
    /// </summary>
    Active,

    /// <summary>
    /// The user account is temporarily disabled and cannot be used.
    /// </summary>
    Disabled,

    /// <summary>
    /// The user account is archived and no longer in active use.
    /// </summary>
    Archived,
}
