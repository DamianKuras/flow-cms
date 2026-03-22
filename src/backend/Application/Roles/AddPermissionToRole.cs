using Application.Interfaces;
using Domain.Common;
using Domain.Permissions;
using Domain.Roles;
using Domain.Users;
using Microsoft.Extensions.Logging;

namespace Application.Roles;

/// <summary>Command to add a permission rule to a role.</summary>
/// <param name="RoleId">The role to grant the permission to.</param>
/// <param name="Action">The CMS action being permitted or denied.</param>
/// <param name="ResourceType">The type of resource this permission applies to.</param>
/// <param name="ResourceId">
/// A specific resource instance to restrict this permission to.
/// When null the permission applies to all resources of the given type.
/// </param>
/// <param name="Scope">Whether the rule grants or denies access.</param>
public sealed record AddPermissionToRoleCommand(
    Guid RoleId,
    CmsAction Action,
    ResourceType ResourceType,
    Guid? ResourceId,
    PermissionScope Scope
);

/// <summary>Handles adding a permission rule to a role.</summary>
public sealed class AddPermissionToRoleCommandHandler(
    IRoleRepository roleRepository,
    IRolePermissionService rolePermissionService,
    IUserContext userContext,
    ILogger<AddPermissionToRoleCommandHandler> logger
) : ICommandHandler<AddPermissionToRoleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        AddPermissionToRoleCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!await userContext.IsInRoleAsync("Admin"))
        {
            logger.LogWarning(
                "Unauthorized attempt to add permission to role {RoleId}",
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

        await rolePermissionService.AddPermissionToRoleAsync(command.RoleId, rule);

        logger.LogInformation(
            "Added {Scope} permission {Action} on {ResourceType} (ResourceId={ResourceId}) to role {RoleId}",
            command.Scope,
            command.Action,
            command.ResourceType,
            command.ResourceId,
            command.RoleId
        );

        return Result<Guid>.Success(command.RoleId);
    }

    private static PermissionRule BuildRule(AddPermissionToRoleCommand command) =>
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
                $"Resource type '{resourceType}' is not supported for permission assignment."
            ),
        };
}
