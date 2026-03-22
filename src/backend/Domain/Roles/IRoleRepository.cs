using Domain.Common;

namespace Domain.Roles;

/// <summary>
/// Repository interface for managing role persistence operations.
/// </summary>
public interface IRoleRepository
{
    /// <summary>Checks whether a role with the given ID exists.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Retrieves a role by its unique identifier.</summary>
    Task<RoleListItem?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Retrieves all roles in the system.</summary>
    Task<IReadOnlyList<RoleListItem>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new role with the specified name.
    /// Returns a <see cref="ErrorTypes.Conflict"/> error if the name is already taken.
    /// </summary>
    Task<Result<Guid>> CreateAsync(string name, CancellationToken ct = default);

    /// <summary>Deletes the role with the specified ID.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Assigns the specified role to a user.</summary>
    Task AssignToUserAsync(Guid roleId, Guid userId, CancellationToken ct = default);

    /// <summary>Removes the specified role from a user.</summary>
    Task RemoveFromUserAsync(Guid roleId, Guid userId, CancellationToken ct = default);
}

/// <summary>Lightweight projection of a role for list responses.</summary>
public record RoleListItem(Guid Id, string Name);
