using Application.Interfaces;
using Domain.Permissions;
using Infrastructure.Data;
using Infrastructure.Persistence.Permissions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Entity Framework Core implementation of the permission provider.
/// </summary>
/// <param name="db">The database context used to access role permissions.</param>
public sealed class EfPermissionProvider(AppDbContext db) : IPermissionProvider
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<PermissionRule>> GetPermissionsAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken ct
    )
    {
        List<RolePermissionEntity> rules = await db
            .RolePermissions.Where(rp => roleIds.Contains(rp.RoleId))
            .ToListAsync(ct);

        return [..rules.Select(ToDomain)];
    }

    private static PermissionRule ToDomain(RolePermissionEntity rp)
    {
        if (rp.ResourceId is null)
        {
            Domain.Permissions.ResourceType domainType = rp.ResourceType switch
            {
                Persistence.Permissions.ResourceType.ContentType =>
                    Domain.Permissions.ResourceType.ContentType,
                Persistence.Permissions.ResourceType.ContentItem =>
                    Domain.Permissions.ResourceType.ContentItem,
                Persistence.Permissions.ResourceType.Field =>
                    Domain.Permissions.ResourceType.Field,
                Persistence.Permissions.ResourceType.User =>
                    Domain.Permissions.ResourceType.User,
                _ => throw new NotSupportedException(
                    $"Unsupported resource type '{rp.ResourceType}'."
                ),
            };

            return PermissionRule.ForResourceType(ActorType.User, rp.Action, domainType, rp.Scope);
        }

        Resource resource = rp.ResourceType switch
        {
            Persistence.Permissions.ResourceType.ContentType =>
                new ContentTypeResource(rp.ResourceId.Value),
            Persistence.Permissions.ResourceType.ContentItem =>
                new ContentItemResource(rp.ResourceId.Value),
            Persistence.Permissions.ResourceType.Field =>
                new FieldResource(rp.ResourceId.Value),
            Persistence.Permissions.ResourceType.User =>
                new UserResource(rp.ResourceId.Value),
            _ => throw new NotSupportedException(
                $"Unsupported resource type '{rp.ResourceType}'."
            ),
        };

        return PermissionRule.ForResource(ActorType.User, rp.Action, resource, rp.Scope);
    }
}
