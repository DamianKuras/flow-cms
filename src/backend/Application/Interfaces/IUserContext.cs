namespace Application.Interfaces;

/// <summary>
/// Provides access to the current user's execution context, including authentication state and roles.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets a value indicating whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the unique identifier of the currently authenticated user.
    /// Throws an exception if the user is not authenticated or the ID claim is missing.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets a read-only collection of Role IDs assigned to the current user.
    /// </summary>
    IReadOnlyCollection<Guid> RoleIds { get; }

    /// <summary>
    /// Asynchronously determines whether the current user belongs to the specified role.
    /// </summary>
    /// <param name="roleName">The name of the role to verify.</param>
    /// <returns>True if the user is in the specified role; otherwise, false.</returns>
    Task<bool> IsInRoleAsync(string roleName);
}
