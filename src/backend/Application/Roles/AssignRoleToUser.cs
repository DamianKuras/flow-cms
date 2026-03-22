using Application.Interfaces;
using Domain.Common;
using Domain.Roles;
using Domain.Users;
using Microsoft.Extensions.Logging;

namespace Application.Roles;

/// <summary>Command to assign a role to a user.</summary>
public sealed record AssignRoleToUserCommand(Guid RoleId, Guid UserId);

/// <summary>Handles assigning a role to a user.</summary>
public sealed class AssignRoleToUserCommandHandler(
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    IUserContext userContext,
    ILogger<AssignRoleToUserCommandHandler> logger
) : ICommandHandler<AssignRoleToUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        AssignRoleToUserCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!await userContext.IsInRoleAsync("Admin"))
        {
            logger.LogWarning(
                "Unauthorized attempt to assign role {RoleId} to user {UserId}",
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

        await roleRepository.AssignToUserAsync(command.RoleId, command.UserId, cancellationToken);

        logger.LogInformation(
            "Assigned role {RoleId} to user {UserId}",
            command.RoleId,
            command.UserId
        );

        return Result<Guid>.Success(command.UserId);
    }
}
