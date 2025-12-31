using Domain.Users;
using Infrastructure.Data;
using Infrastructure.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for user persistence operations.
/// Coordinates between domain entities and ASP.NET Core Identity.
/// </summary>
/// <param name="db">The application database context.</param>
/// <param name="userManager">The ASP.NET Core Identity user manager.</param>
public sealed class UserRepository(AppDbContext db, UserManager<AppUser> userManager)
    : IUserRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(User user, string password, CancellationToken ct = default)
    {
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(ct);

        try
        {
            // Add domain user.
            db.DomainUsers.Add(user);
            await db.SaveChangesAsync(ct);

            // Create identity user.
            var appUser = new AppUser
            {
                Id = user.Id,
                UserName = user.Email,
                Email = user.Email,
                EmailConfirmed = false,
            };

            IdentityResult result = await userManager.CreateAsync(appUser, password);

            if (!result.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create identity user: {errors}");
            }

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await db.DomainUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

    /// <inheritdoc/>
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.DomainUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
}
