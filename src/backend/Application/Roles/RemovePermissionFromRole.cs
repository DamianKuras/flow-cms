using Application.Interfaces;
using Domain.Common;
using Domain.Permissions;
using Domain.Roles;
using Domain.Users;
using Microsoft.Extensions.Logging;

namespace Application.Roles;

/// <summary>Command to remove a permission rule from a role.</summary>
public sealed record RemovePermissionFromRoleCommand(
    Guid RoleId,
    CmsAction Action,
    ResourceType ResourceType,
    Guid? ResourceId,
    PermissionScope Scope
);

/// <summary>Handles removing a permission rule from a role.</summary>
public sealed class RemovePermissionFromRoleCommandHandler(
    IRoleRepository roleRepository,
    IRolePermissionService rolePermissionService,
    IUserContext userContext,
    ILogger<RemovePermissionFromRoleCommandHandler> logger
) : ICommandHandler<RemovePermissionFromRoleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        RemovePermissionFromRoleCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!await userContext.IsInRoleAsync("Admin"))
        {
            logger.LogWarning(
                "Unauthorized attempt to remove permission from role {RoleId}",
                command.RoleId
            );
            return Result<Guid>.Failure(
                Error.Forbidden("Only admins can manage role permissions.")
            );
        }

        RoleListItem? role = await roleRepository.GetByIdAsync(command.RoleId, cancellationToken);

        if (role is null)
        {
            return Result<Guid>.Failure(
                Error.NotFound($"Role with id {command.RoleId} not found.")
            );
        }

        PermissionRule rule = BuildRule(command);

        await rolePermissionService.RemovePermissionFromRoleAsync(command.RoleId, rule);

        logger.LogInformation(
            "Removed {Scope} permission {Action} on {ResourceType} (ResourceId={ResourceId}) from role {RoleId}",
            command.Scope,
            command.Action,
            command.ResourceType,
            command.ResourceId,
            command.RoleId
        );

        return Result<Guid>.Success(command.RoleId);
    }

    private static PermissionRule BuildRule(RemovePermissionFromRoleCommand command) =>
        command.ResourceId is not null
            ? PermissionRule.ForResource(
                ActorType.User,
                command.Action,
                ToResource(command.ResourceType, command.ResourceId.Value),
                command.Scope
            )
            : PermissionRule.ForResourceType(
                ActorType.User,
                command.Action,
                command.ResourceType,
                command.Scope
            );

    private static Resource ToResource(ResourceType resourceType, Guid resourceId) =>
        resourceType switch
        {
            ResourceType.ContentType => new ContentTypeResource(resourceId),
            ResourceType.ContentItem => new ContentItemResource(resourceId),
            ResourceType.Field => new FieldResource(resourceId),
            _ => throw new NotSupportedException(
                $"Resource type '{resourceType}' is not supported for permission removal."
            ),
        };
}
