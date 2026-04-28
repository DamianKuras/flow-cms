using Application.Interfaces;
using Domain.Common;
using Domain.ContentItems;
using Domain.Permissions;
using Domain.Roles;
using Domain.Users;
using Microsoft.Extensions.Logging;

namespace Application.Roles;

/// <summary>Query to retrieve a single role with its permissions.</summary>
public sealed record GetRoleQuery(Guid Id);

/// <summary>A permission rule attached to a role, suitable for API responses.</summary>
public sealed record PermissionDto(
    CmsAction Action,
    ResourceType ResourceType,
    string? ResourceId,
    string? ResourceName,
    PermissionScope Scope
);

/// <summary>Response containing a role's details and assigned permissions.</summary>
public sealed record GetRoleResponse(
    Guid Id,
    string Name,
    IReadOnlyList<PermissionDto> Permissions
);

/// <summary>Handles the retrieval of a role and its permissions.</summary>
public sealed class GetRoleQueryHandler(
    IRoleRepository roleRepository,
    IPermissionProvider permissionProvider,
    IContentItemRepository contentItemRepository,
    IUserContext userContext,
    ILogger<GetRoleQueryHandler> logger
) : IQueryHandler<GetRoleQuery, GetRoleResponse>
{
    public async Task<Result<GetRoleResponse>> Handle(
        GetRoleQuery query,
        CancellationToken cancellationToken
    )
    {
        if (!await userContext.IsInRoleAsync("Admin"))
        {
            logger.LogWarning("Unauthorized attempt to get role {RoleId}", query.Id);
            return Result<GetRoleResponse>.Failure(Error.Forbidden("Only admins can view roles."));
        }

        RoleListItem? role = await roleRepository.GetByIdAsync(query.Id, cancellationToken);

        if (role is null)
        {
            return Result<GetRoleResponse>.Failure(
                Error.NotFound($"Role with id {query.Id} not found.")
            );
        }

        IReadOnlyCollection<PermissionRule> permissions =
            await permissionProvider.GetPermissionsAsync([query.Id], cancellationToken);

        Dictionary<string, string> resourceNames = await ResolveResourceNamesAsync(
            permissions,
            cancellationToken
        );

        var permissionDtos = permissions
            .Select(p =>
            {
                string? resourceId = GetResourceId(p.Resource);
                return new PermissionDto(
                    p.Action,
                    p.Resource?.Type ?? p.ResourceType!.Value,
                    resourceId,
                    resourceId is not null ? resourceNames.GetValueOrDefault(resourceId) : null,
                    p.Scope
                );
            })
            .ToList();

        return Result<GetRoleResponse>.Success(
            new GetRoleResponse(role.Id, role.Name, permissionDtos)
        );
    }

    private async Task<Dictionary<string, string>> ResolveResourceNamesAsync(
        IReadOnlyCollection<PermissionRule> permissions,
        CancellationToken ct
    )
    {
        var result = new Dictionary<string, string>();

        // Content type name IS the resourceId — no lookup needed.
        foreach (var p in permissions.Where(p => p.Resource is ContentTypeResource))
        {
            var name = ((ContentTypeResource)p.Resource!).Name;
            result[name] = name;
        }

        var contentItemIds = permissions
            .Where(p => p.Resource is ContentItemResource)
            .Select(p => ((ContentItemResource)p.Resource!).ContentItemId)
            .Distinct()
            .ToList();

        ContentItem?[] contentItems = await Task.WhenAll(
            contentItemIds.Select(id => contentItemRepository.GetByIdAsync(id, ct))
        );

        foreach ((Guid id, ContentItem? ci) in contentItemIds.Zip(contentItems))
        {
            if (ci is not null)
                result[id.ToString()] = ci.Title;
        }

        return result;
    }

    private static string? GetResourceId(Resource? resource) =>
        resource switch
        {
            ContentTypeResource r => r.Name,
            ContentItemResource r => r.ContentItemId.ToString(),
            FieldResource r => r.FieldId.ToString(),
            UserResource r => r.UserId.ToString(),
            _ => null,
        };
}
