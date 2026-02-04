namespace Domain.Users;

/// <summary>
/// Represents a user entity in the domain model.
/// </summary>
public sealed class User
{
    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    private User() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class with the specified values.
    /// </summary>
    /// <param name="id">The unique identifier for the user.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <exception cref="ArgumentException">Thrown when email or display name is null or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown when the id is empty.</exception>
    public User(Guid id, string email, string displayName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
        }

        Id = id;
        Email = email;
        DisplayName = displayName;
        Status = UserStatus.Active;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the unique identifier for the user.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    public string Email { get; private set; } = "";

    /// <summary>
    /// Gets the user's display name.
    /// </summary>
    public string DisplayName { get; private set; } = "";

    /// <summary>
    /// Gets the current status of the user account.
    /// </summary>
    public UserStatus Status { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    ///Private collection of role identifiers assigned to the user.
    /// </summary>
    private readonly HashSet<Guid> _roles = [];

    /// <summary>
    /// Gets a read-only collection of role identifiers assigned to the user.
    /// </summary>
    public IReadOnlyCollection<Guid> Roles => _roles;

    /// <summary>
    /// Changes the user's display name.
    /// </summary>
    /// <param name="displayName">The new display name for the user.</param>
    /// <exception cref="ArgumentException">Thrown when the display name is null, empty, or whitespace.</exception>
    public void Rename(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name cannot be empty");
        }

        DisplayName = displayName;
    }

    /// <summary>
    /// Assigns a role to the user.
    /// </summary>
    /// <param name="role">The unique identifier of the role to assign.</param>
    public void AssignRole(Guid role) => _roles.Add(role);

    /// <summary>
    /// Removes a role from the user.
    /// </summary>
    /// <param name="role">The unique identifier of the role to remove.</param>
    public void RemoveRole(Guid role) => _roles.Remove(role);

    /// <summary>
    /// Deactivates the user account by setting the status to disabled.
    /// </summary>
    public void Deactivate() => Status = UserStatus.Disabled;

    /// <summary>
    /// Determines whether the user possesses any of the specified administrative roles.
    /// </summary>
    /// <param name="adminRoleNames">A collection of role identifiers that represent administrative privileges.</param>
    /// <returns><c>true</c> if the user is assigned at least one of the provided role IDs; otherwise, <c>false</c>.</returns>
    public bool IsAdmin(IEnumerable<string> adminRoleNames) =>
        Roles.Any(r => adminRoleNames.Contains(r.ToString(), StringComparer.OrdinalIgnoreCase));
}

/// <summary>
/// A lightweight record representing a user in a paginated list or data transfer object.
/// </summary>
/// <param name="Id">The unique identifier of the user.</param>
/// <param name="Email">The email address of the user.</param>
/// <param name="DisplayName">The display name of the user.</param>
/// <param name="Status">The string representation of the user's status (e.g., "Active", "Disabled").</param>
/// <param name="CreatedAt">The UTC date and time when the user was created.</param>
public record PagedUser(
    Guid Id,
    string Email,
    string DisplayName,
    string Status,
    DateTime CreatedAt
);
