using Application.Interfaces;
using Domain.Common;
using Domain.Roles;
using Domain.Users;
using Microsoft.Extensions.Logging;

namespace Application.Roles;

/// <summary>Command to create a new role.</summary>
public sealed record CreateRoleCommand(string Name);

/// <summary>Handles the creation of a new role.</summary>
public sealed class CreateRoleCommandHandler(
    IRoleRepository roleRepository,
    IUserContext userContext,
    ILogger<CreateRoleCommandHandler> logger
) : ICommandHandler<CreateRoleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateRoleCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!await userContext.IsInRoleAsync("Admin"))
        {
            logger.LogWarning("Unauthorized attempt to create role '{Name}'", command.Name);
            return Result<Guid>.Failure(Error.Forbidden("Only admins can manage roles."));
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result<Guid>.Failure(Error.Validation("Role name cannot be empty."));
        }

        Result<Guid> result = await roleRepository.CreateAsync(command.Name, cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation("Created role '{Name}' with Id {Id}", command.Name, result.Value);
        }

        return result;
    }
}
