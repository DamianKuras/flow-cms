using Application.Interfaces;
using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
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
    Guid? ResourceId,
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
    IContentTypeRepository contentTypeRepository,
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

        Dictionary<Guid, string> resourceNames = await ResolveResourceNamesAsync(
            permissions,
            cancellationToken
        );

        var permissionDtos = permissions
            .Select(p =>
            {
                Guid? resourceId = GetResourceId(p.Resource);
                return new PermissionDto(
                    p.Action,
                    p.Resource?.Type ?? p.ResourceType!.Value,
                    resourceId,
                    resourceId.HasValue ? resourceNames.GetValueOrDefault(resourceId.Value) : null,
                    p.Scope
                );
            })
            .ToList();

        return Result<GetRoleResponse>.Success(
            new GetRoleResponse(role.Id, role.Name, permissionDtos)
        );
    }

    private async Task<Dictionary<Guid, string>> ResolveResourceNamesAsync(
        IReadOnlyCollection<PermissionRule> permissions,
        CancellationToken ct
    )
    {
        var contentTypeIds = permissions
            .Where(p => p.Resource is ContentTypeResource)
            .Select(p => ((ContentTypeResource)p.Resource!).ContentTypeId)
            .Distinct()
            .ToList();

        var contentItemIds = permissions
            .Where(p => p.Resource is ContentItemResource)
            .Select(p => ((ContentItemResource)p.Resource!).ContentItemId)
            .Distinct()
            .ToList();

        ContentType?[] contentTypes = await Task.WhenAll(
            contentTypeIds.Select(id => contentTypeRepository.GetByIdAsync(id, ct))
        );

        ContentItem?[] contentItems = await Task.WhenAll(
            contentItemIds.Select(id => contentItemRepository.GetByIdAsync(id, ct))
        );

        var result = new Dictionary<Guid, string>();

        foreach ((Guid id, ContentType? ct2) in contentTypeIds.Zip(contentTypes))
        {
            if (ct2 is not null)
            {
                result[id] = ct2.Name;
            }
        }

        foreach ((Guid id, ContentItem? ci) in contentItemIds.Zip(contentItems))
        {
            if (ci is not null)
            {
                result[id] = ci.Title;
            }
        }

        return result;
    }

    private static Guid? GetResourceId(Resource? resource) =>
        resource switch
        {
            ContentTypeResource r => r.ContentTypeId,
            ContentItemResource r => r.ContentItemId,
            FieldResource r => r.FieldId,
            UserResource r => r.UserId,
            _ => null,
        };
}
