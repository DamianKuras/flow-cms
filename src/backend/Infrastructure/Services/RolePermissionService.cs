using Application.Interfaces;
using Domain.Permissions;
using Infrastructure.Data;
using Infrastructure.Persistence.Permissions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Service responsible for managing permissions assigned to roles.
/// </summary>
/// <remarks>
/// Interacts with the underlying AppDbContext to persist role permissions.
/// </remarks>
/// <param name="db">The database context used for data operations.</param>
public class RolePermissionService(AppDbContext db) : IRolePermissionService
{
    private readonly AppDbContext _db = db;

    /// <summary>
    /// Adds a new permission rule for the specified role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role.</param>
    /// <param name="rule">The permission rule to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task AddPermissionToRoleAsync(Guid roleId, PermissionRule rule)
    {
        _db.RolePermissions.Add(
            new RolePermissionEntity
            {
                RoleId = roleId,
                Action = rule.Action,
                ResourceType = ToResourceType(rule.Resource),
                ResourceId = ToResourceId(rule.Resource),
                Scope = rule.Scope,
            }
        );

        await _db.SaveChangesAsync();
    }

    private Guid? ToResourceId(Resource resource) => resource switch
    {
        ContentTypeResource c => c.ContentTypeId,
        ContentItemResource c => c.ContentItemId,
        FieldResource c => c.FieldId,
        _ => throw new NotSupportedException($"Resource type {resource.GetType()} has no mapping to an ID.")
    };

    private Persistence.Permissions.ResourceType ToResourceType(Resource resource) => resource switch
    {
        ContentTypeResource => Persistence.Permissions.ResourceType.ContentType,
        ContentItemResource => Persistence.Permissions.ResourceType.ContentItem,
        FieldResource => Persistence.Permissions.ResourceType.Field,
        _ => throw new NotSupportedException($"Resource type {resource.GetType()} is unsupported for Persistence.")
    };

    /// <summary>
    /// Removes an existing permission rule from the specified role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role.</param>
    /// <param name="rule">The permission rule to remove.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RemovePermissionFromRoleAsync(Guid roleId, PermissionRule rule)
    {
        var resourceType = ToResourceType(rule.Resource);
        var resourceId = ToResourceId(rule.Resource);

        var entity = await _db.RolePermissions.FirstOrDefaultAsync(rp => 
            rp.RoleId == roleId && 
            rp.Action == rule.Action && 
            rp.ResourceType == resourceType && 
            rp.ResourceId == resourceId &&
            rp.Scope == rule.Scope);

        if (entity is not null)
        {
            _db.RolePermissions.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }
}
