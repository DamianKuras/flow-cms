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
    public User(Guid id, string email, string displayName)
    {
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
}
