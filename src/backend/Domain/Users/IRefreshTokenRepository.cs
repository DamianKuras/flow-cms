namespace Domain.Users;

/// <summary>
/// Repository interface for managing refresh tokens in the system.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Retrieves a refresh token by its token value.
    /// </summary>
    /// <param name="token">The token value to search for.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>The refresh token if found; otherwise, null.</returns>
    Task<RefreshToken?> GetByTokenValueAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Adds a new refresh token to the repository.
    /// </summary>
    /// <param name="refreshToken">The refresh token to add.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Persists all pending changes to the database.
    /// </summary>
    /// <param name="ct">Cancellation token for async operation.</param>
    Task SaveChangesAsync(CancellationToken ct = default);
}
