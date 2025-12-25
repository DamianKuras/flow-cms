using Domain.Users;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default) =>
        await db.RefreshTokens.AddAsync(refreshToken, ct);

    public async Task<RefreshToken?> GetByTokenValueAsync(
        string token,
        CancellationToken ct = default
    ) => await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync();
}
