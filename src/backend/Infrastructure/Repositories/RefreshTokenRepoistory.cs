using Domain.Users;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository for managing refresh token persistence and retrieval operations.
/// </summary>
/// <param name="db">
/// The application database context that provides access to the underlying Entity Framework Core DbSet collections
/// and persistence services.
/// </param>
public sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    /// <summary>
    /// Asynchronously adds a new refresh token to the repository without immediately persisting it to the database.
    /// </summary>
    /// <param name="refreshToken">The refresh token entity to be added.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A completed task representing the asynchronous add operation. The token is added to the DbSet's change tracker
    /// but is not persisted to the database until SaveChangesAsync is explicitly called.
    /// </returns>
    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default) =>
        await db.RefreshTokens.AddAsync(refreshToken, ct);

    /// <summary>
    /// Asynchronously retrieves a refresh token from the repository by its token value.
    /// </summary>
    /// <param name="token">The string representation of the refresh token to search for</param>
    /// <param name="ct"> Cancellation token that can be used to cancel the asynchronous database query. </param>
    /// <returns>A task that resolves to the matching RefreshToken entity if found, or null if no token with the specified
    /// value exists in the repository.</returns>
    public async Task<RefreshToken?> GetByTokenValueAsync(
        string token,
        CancellationToken ct = default
    ) => await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);

    /// <summary>
    /// Asynchronously persists all pending changes in the repository to the database.
    /// </summary>
    /// <param name="ct"> A cancellation token that can be used to cancel the save operation.</param>
    /// <returns>A task that completes when all changes have been successfully committed to the database.</returns>
    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync();
}
