using Application.Interfaces;
using Domain.Permissions;
using Infrastructure.Data;
using Infrastructure.Persistence.Permissions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Service responsible for managing permissions assigned to roles.
/// </summary>
/// <param name="db">The database context used for data operations.</param>
public sealed class RolePermissionService(AppDbContext db) : IRolePermissionService
{
    /// <inheritdoc/>
    public async Task AddPermissionToRoleAsync(Guid roleId, PermissionRule rule)
    {
        Persistence.Permissions.ResourceType resourceType = ToResourceType(rule);
        string? resourceId = ToResourceId(rule);

        bool exists = await db.RolePermissions.AnyAsync(rp =>
            rp.RoleId == roleId
            && rp.Action == rule.Action
            && rp.ResourceType == resourceType
            && rp.ResourceId == resourceId
            && rp.Scope == rule.Scope
        );

        if (exists)
        {
            return;
        }

        db.RolePermissions.Add(
            new RolePermissionEntity
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                Action = rule.Action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Scope = rule.Scope,
            }
        );

        await db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task RemovePermissionFromRoleAsync(Guid roleId, PermissionRule rule)
    {
        Persistence.Permissions.ResourceType resourceType = ToResourceType(rule);
        string? resourceId = ToResourceId(rule);

        RolePermissionEntity? entity = await db.RolePermissions.FirstOrDefaultAsync(rp =>
            rp.RoleId == roleId
            && rp.Action == rule.Action
            && rp.ResourceType == resourceType
            && rp.ResourceId == resourceId
            && rp.Scope == rule.Scope
        );

        if (entity is not null)
        {
            db.RolePermissions.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    private static string? ToResourceId(PermissionRule rule) =>
        rule.Resource switch
        {
            ContentTypeResource r => r.Name,
            ContentItemResource r => r.ContentItemId.ToString(),
            FieldResource r => r.FieldId.ToString(),
            UserResource r => r.UserId.ToString(),
            null => null,
            _ => throw new NotSupportedException(
                $"Resource type '{rule.Resource.GetType().Name}' has no ID mapping."
            ),
        };

    private static Persistence.Permissions.ResourceType ToResourceType(PermissionRule rule)
    {
        if (rule.Resource is not null)
        {
            return rule.Resource switch
            {
                ContentTypeResource => Persistence.Permissions.ResourceType.ContentType,
                ContentItemResource => Persistence.Permissions.ResourceType.ContentItem,
                FieldResource => Persistence.Permissions.ResourceType.Field,
                UserResource => Persistence.Permissions.ResourceType.User,
                _ => throw new NotSupportedException(
                    $"Resource type '{rule.Resource.GetType().Name}' is unsupported."
                ),
            };
        }

        return rule.ResourceType switch
        {
            Domain.Permissions.ResourceType.ContentType => Persistence
                .Permissions
                .ResourceType
                .ContentType,
            Domain.Permissions.ResourceType.ContentItem => Persistence
                .Permissions
                .ResourceType
                .ContentItem,
            Domain.Permissions.ResourceType.Field => Persistence.Permissions.ResourceType.Field,
            Domain.Permissions.ResourceType.User => Persistence.Permissions.ResourceType.User,
            _ => throw new NotSupportedException(
                $"Resource type '{rule.ResourceType}' is unsupported."
            ),
        };
    }
}
