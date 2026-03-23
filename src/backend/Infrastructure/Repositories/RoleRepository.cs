using Domain.Common;
using Domain.Roles;
using Infrastructure.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for role management using ASP.NET Core Identity.
/// </summary>
public sealed class RoleRepository(
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager
) : IRoleRepository
{
    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        AppRole? role = await roleManager.FindByIdAsync(id.ToString());
        return role is not null;
    }

    /// <inheritdoc/>
    public async Task<RoleListItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        AppRole? role = await roleManager.FindByIdAsync(id.ToString());
        return role is null ? null : new RoleListItem(role.Id, role.Name!);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RoleListItem>> GetAllAsync(CancellationToken ct = default) =>
        await roleManager.Roles.Select(r => new RoleListItem(r.Id, r.Name!)).ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Result<Guid>> CreateAsync(string name, CancellationToken ct = default)
    {
        if (await roleManager.FindByNameAsync(name) is not null)
        {
            return Result<Guid>.Failure(Error.Conflict($"Role '{name}' already exists."));
        }

        var role = new AppRole { Name = name };
        IdentityResult result = await roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<Guid>.Failure(Error.Validation(errors));
        }

        return Result<Guid>.Success(role.Id);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        AppRole? role = await roleManager.FindByIdAsync(id.ToString());

        if (role is null)
        {
            return;
        }

        IdentityResult result = await roleManager.DeleteAsync(role);

        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to delete role: {errors}");
        }
    }

    /// <inheritdoc/>
    public async Task AssignToUserAsync(Guid roleId, Guid userId, CancellationToken ct = default)
    {
        AppRole role =
            await roleManager.FindByIdAsync(roleId.ToString())
            ?? throw new InvalidOperationException($"Role {roleId} not found.");

        AppUser user =
            await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");

        if (!await userManager.IsInRoleAsync(user, role.Name!))
        {
            await userManager.AddToRoleAsync(user, role.Name!);
        }
    }

    /// <inheritdoc/>
    public async Task RemoveFromUserAsync(Guid roleId, Guid userId, CancellationToken ct = default)
    {
        AppRole role =
            await roleManager.FindByIdAsync(roleId.ToString())
            ?? throw new InvalidOperationException($"Role {roleId} not found.");

        AppUser user =
            await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");

        if (await userManager.IsInRoleAsync(user, role.Name!))
        {
            await userManager.RemoveFromRoleAsync(user, role.Name!);
        }
    }
}
