using Api.Extensions;
using Application.Interfaces;
using Application.Roles;
using Domain.Common;
using Domain.Users;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

/// <summary>
/// Provides extension methods for registering role management API endpoints.
/// </summary>
public static class RolesEndpoints
{
    /// <summary>
    /// Registers the sub-route endpoints for roles (/roles).
    /// </summary>
    public static void RegisterRolesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        IEndpointRouteBuilder group = endpoints.MapGroup("/roles").WithTags("Roles");

        group
            .MapGet("/", GetRoles)
            .WithName("Get Roles")
            .RequireAuthorization()
            .Produces<GetRolesResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);

        group
            .MapGet("/{id:guid}", GetRole)
            .WithName("Get Role")
            .RequireAuthorization()
            .Produces<GetRoleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        group
            .MapPost("/", CreateRole)
            .WithName("Create Role")
            .RequireAuthorization()
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden);

        group
            .MapDelete("/{id:guid}", DeleteRole)
            .WithName("Delete Role")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status403Forbidden);

        group
            .MapPost("/{roleId:guid}/users/{userId:guid}", AssignRoleToUser)
            .WithName("Assign Role To User")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        group
            .MapDelete("/{roleId:guid}/users/{userId:guid}", RemoveRoleFromUser)
            .WithName("Remove Role From User")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        group
            .MapPost("/{roleId:guid}/permissions", AddPermission)
            .WithName("Add Permission To Role")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        group
            .MapDelete("/{roleId:guid}/permissions", RemovePermission)
            .WithName("Remove Permission From Role")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetRoles(
        [FromServices] IQueryHandler<GetRolesQuery, GetRolesResponse> handler,
        CancellationToken cancellationToken
    )
    {
        Result<GetRolesResponse> result = await handler.Handle(new GetRolesQuery(), cancellationToken);
        return result.Match(onSuccess: Results.Ok);
    }

    private static async Task<IResult> GetRole(
        Guid id,
        [FromServices] IQueryHandler<GetRoleQuery, GetRoleResponse> handler,
        CancellationToken cancellationToken
    )
    {
        Result<GetRoleResponse> result = await handler.Handle(new GetRoleQuery(id), cancellationToken);
        return result.Match(onSuccess: Results.Ok);
    }

    private static async Task<IResult> CreateRole(
        [FromBody] CreateRoleCommand command,
        [FromServices] ICommandHandler<CreateRoleCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(onSuccess: id => Results.Created($"/api/roles/{id}", new { Id = id }));
    }

    private static async Task<IResult> DeleteRole(
        Guid id,
        [FromServices] ICommandHandler<DeleteRoleCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        Result<Guid> result = await handler.Handle(new DeleteRoleCommand(id), cancellationToken);
        return result.Match(onSuccess: _ => Results.NoContent());
    }

    private static async Task<IResult> AssignRoleToUser(
        Guid roleId,
        Guid userId,
        [FromServices] ICommandHandler<AssignRoleToUserCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        Result<Guid> result = await handler.Handle(
            new AssignRoleToUserCommand(roleId, userId),
            cancellationToken
        );
        return result.Match(onSuccess: _ => Results.NoContent());
    }

    private static async Task<IResult> RemoveRoleFromUser(
        Guid roleId,
        Guid userId,
        [FromServices] ICommandHandler<RemoveRoleFromUserCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        Result<Guid> result = await handler.Handle(
            new RemoveRoleFromUserCommand(roleId, userId),
            cancellationToken
        );
        return result.Match(onSuccess: _ => Results.NoContent());
    }

    private static async Task<IResult> AddPermission(
        Guid roleId,
        [FromBody] AddPermissionToRoleCommand command,
        [FromServices] ICommandHandler<AddPermissionToRoleCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        // RoleId comes from the route; overwrite whatever was in the body.
        Result<Guid> result = await handler.Handle(
            command with { RoleId = roleId },
            cancellationToken
        );
        return result.Match(onSuccess: _ => Results.NoContent());
    }

    private static async Task<IResult> RemovePermission(
        Guid roleId,
        [FromBody] RemovePermissionFromRoleCommand command,
        [FromServices] ICommandHandler<RemovePermissionFromRoleCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        Result<Guid> result = await handler.Handle(
            command with { RoleId = roleId },
            cancellationToken
        );
        return result.Match(onSuccess: _ => Results.NoContent());
    }
}
