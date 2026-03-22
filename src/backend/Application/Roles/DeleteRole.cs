using Application.Interfaces;
using Domain.Common;
using Domain.Roles;
using Domain.Users;
using Microsoft.Extensions.Logging;

namespace Application.Roles;

/// <summary>Command to delete a role by its unique identifier.</summary>
public sealed record DeleteRoleCommand(Guid Id);

/// <summary>Handles the deletion of a role.</summary>
public sealed class DeleteRoleCommandHandler(
    IRoleRepository roleRepository,
    IUserContext userContext,
    ILogger<DeleteRoleCommandHandler> logger
) : ICommandHandler<DeleteRoleCommand, Guid>
{
    private const string ADMIN_ROLE_NAME = "Admin";

    public async Task<Result<Guid>> Handle(
        DeleteRoleCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!await userContext.IsInRoleAsync(ADMIN_ROLE_NAME))
        {
            logger.LogWarning("Unauthorized attempt to delete role {RoleId}", command.Id);
            return Result<Guid>.Failure(Error.Forbidden("Only admins can manage roles."));
        }

        RoleListItem? role = await roleRepository.GetByIdAsync(command.Id, cancellationToken);

        if (role is null)
        {
            return Result<Guid>.Failure(Error.NotFound($"Role with id {command.Id} not found."));
        }

        if (role.Name.Equals(ADMIN_ROLE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            return Result<Guid>.Failure(Error.Conflict("The Admin role cannot be deleted."));
        }

        await roleRepository.DeleteAsync(command.Id, cancellationToken);

        logger.LogInformation("Deleted role {RoleId}", command.Id);

        return Result<Guid>.Success(command.Id);
    }
}
