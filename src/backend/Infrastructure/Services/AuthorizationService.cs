using Application.Auth;
using Application.Interfaces;
using Domain.Common;
using Domain.Permissions;
using Domain.Users;
using Infrastructure.Data;
using Infrastructure.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Implements authorization logic for CMS resources by evaluating user permissions and roles.
/// </summary>
/// <param name="userContext">Provides information about the current authenticated user.</param>
/// <param name="permissions">Retrieves permission rules for roles.</param>
/// <param name="evaluator">Evaluates permission rules against actions and resources.</param>
/// <param name="logger">Logger for diagnostic information.</param>
public sealed class AuthorizationService(
    IUserContext userContext,
    IPermissionProvider permissions,
    IPermissionEvaluator evaluator,
    ILogger<AuthorizationService> logger
) : IAuthorizationService
{
    private const string ADMIN_ROLE_NAME = "Admin";

    /// <inheritdoc/>
    public async Task<bool> IsAllowedAsync(
        CmsAction action,
        Resource resource,
        CancellationToken ct
    )
    {
        if (!userContext.IsAuthenticated)
        {
            logger.LogDebug(
                "Authorization denied: User is not authenticated for action {Action} on resource {Resource}",
                action,
                resource
            );
            return false;
        }

        if (await userContext.IsInRoleAsync(ADMIN_ROLE_NAME))
        {
            logger.LogDebug(
                "Authorization granted: User {UserId} is in Admin role",
                userContext.UserId
            );
            return true;
        }

        IReadOnlyCollection<PermissionRule> rules = await permissions.GetPermissionsAsync(
            userContext.RoleIds,
            ct
        );

        var actor = new UserActor(userContext.UserId);

        bool isAllowed = evaluator.IsAllowed(actor, action, resource, rules);

        if (!isAllowed)
        {
            logger.LogWarning(
                "Authorization denied: User {UserId} not permitted to perform {Action} on {Resource}",
                userContext.UserId,
                action,
                resource
            );
        }

        return isAllowed;
    }

    /// <inheritdoc/>
    public async Task<bool> IsAllowedForTypeAsync(
        CmsAction action,
        ResourceType resourceType,
        CancellationToken ct
    )
    {
        if (!userContext.IsAuthenticated)
        {
            logger.LogDebug(
                "Authorization denied: User is not authenticated for action {Action} on all resources of type {ResourceType}",
                action,
                resourceType
            );
            return false;
        }

        if (await userContext.IsInRoleAsync(ADMIN_ROLE_NAME))
        {
            logger.LogDebug(
                "Authorization granted: User {UserId} is in Admin role",
                userContext.UserId
            );
            return true;
        }

        IReadOnlyCollection<PermissionRule> rules = await permissions.GetPermissionsAsync(
            userContext.RoleIds,
            ct
        );

        var actor = new UserActor(userContext.UserId);

        bool isAllowed = evaluator.IsAllowedForAll(actor, action, resourceType, rules);

        if (!isAllowed)
        {
            logger.LogWarning(
                "Authorization denied: User {UserId} not permitted to perform {Action} on all resources of type {ResourceType}",
                userContext.UserId,
                action,
                resourceType
            );
        }

        return isAllowed;
    }
}
