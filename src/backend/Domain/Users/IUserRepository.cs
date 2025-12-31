namespace Domain.Users;

/// <summary>
/// Repository interface for managing user persistence operations.
/// Handles both domain user entities and identity authentication.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Creates a new user with the specified password.
    /// This operation creates both the domain entity and the identity user.
    /// </summary>
    /// <param name="user">The domain user entity to add.</param>
    /// <param name="password">The password for authentication.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when user creation fails.</exception>
    Task AddAsync(User user, string password, CancellationToken ct = default);
}
