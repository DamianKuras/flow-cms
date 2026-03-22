using Application.Interfaces;
using Domain.Common;
using Domain.Roles;
using Domain.Users;
using Microsoft.Extensions.Logging;

namespace Application.Roles;

/// <summary>Command to remove a role from a user.</summary>
public sealed record RemoveRoleFromUserCommand(Guid RoleId, Guid UserId);

/// <summary>Handles removing a role from a user.</summary>
public sealed class RemoveRoleFromUserCommandHandler(
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    IUserContext userContext,
    ILogger<RemoveRoleFromUserCommandHandler> logger
) : ICommandHandler<RemoveRoleFromUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        RemoveRoleFromUserCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!await userContext.IsInRoleAsync("Admin"))
        {
            logger.LogWarning(
                "Unauthorized attempt to remove role {RoleId} from user {UserId}",
                command.RoleId,
                command.UserId
            );
            return Result<Guid>.Failure(
                Error.Forbidden("Only admins can manage role assignments.")
            );
        }

        RoleListItem? role = await roleRepository.GetByIdAsync(command.RoleId, cancellationToken);

        if (role is null)
        {
            return Result<Guid>.Failure(
                Error.NotFound($"Role with id {command.RoleId} not found.")
            );
        }

        User? user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result<Guid>.Failure(
                Error.NotFound($"User with id {command.UserId} not found.")
            );
        }

        await roleRepository.RemoveFromUserAsync(command.RoleId, command.UserId, cancellationToken);

        logger.LogInformation(
            "Removed role {RoleId} from user {UserId}",
            command.RoleId,
            command.UserId
        );

        return Result<Guid>.Success(command.UserId);
    }
}
