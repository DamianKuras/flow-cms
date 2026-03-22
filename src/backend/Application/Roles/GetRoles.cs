using Application.Interfaces;
using Domain.Common;
using Domain.Roles;
using Domain.Users;
using Microsoft.Extensions.Logging;

namespace Application.Roles;

/// <summary>Query to retrieve all roles in the system.</summary>
public sealed record GetRolesQuery;

/// <summary>Response containing all roles.</summary>
public sealed record GetRolesResponse(IReadOnlyList<RoleListItem> Roles);

/// <summary>Handles the retrieval of all roles.</summary>
public sealed class GetRolesQueryHandler(
    IRoleRepository roleRepository,
    IUserContext userContext,
    ILogger<GetRolesQueryHandler> logger
) : IQueryHandler<GetRolesQuery, GetRolesResponse>
{
    public async Task<Result<GetRolesResponse>> Handle(
        GetRolesQuery query,
        CancellationToken cancellationToken
    )
    {
        if (!await userContext.IsInRoleAsync("Admin"))
        {
            logger.LogWarning("Unauthorized attempt to list roles");
            return Result<GetRolesResponse>.Failure(Error.Forbidden("Only admins can view roles."));
        }

        IReadOnlyList<RoleListItem> roles = await roleRepository.GetAllAsync(cancellationToken);

        return Result<GetRolesResponse>.Success(new GetRolesResponse(roles));
    }
}
