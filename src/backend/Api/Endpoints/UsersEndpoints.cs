using Api.Extensions;
using Api.Responses;
using Application.Auth;
using Application.Interfaces;
using Application.Users;
using Domain.Common;
using Domain.Users;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

/// <summary>
/// Provides extension methods for registering user-related API endpoints.
/// </summary>
public static class UsersEndpoints
{
    /// <summary>
    /// Registers the sub-route endpoints for users (/users).
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    public static void RegisterUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        IEndpointRouteBuilder group = endpoints.MapGroup("/users").WithTags("Users");

        group
            .MapPost("/", CreateUser)
            .WithName("Create User")
            .RequireAuthorization()
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group
            .MapGet("/", GetUsers)
            .WithName("Get Users")
            .RequireAuthorization()
            .Produces<PagedResponse<PagedUser>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> CreateUser(
        [FromBody] CreateUserCommand command,
        [FromServices] ICommandHandler<CreateUserCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        Result<Guid> response = await handler.Handle(command, cancellationToken);
        return response.Match(onSuccess: userId =>
            Results.Created($"/api/users/{userId}", new { Id = userId })
        );
    }

    private static async Task<IResult> GetUsers(
        [FromServices] IQueryHandler<GetUsersQuery, GetUsersResponse> handler,
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10
    )
    {
        var query = new GetUsersQuery(new PaginationParameters(pageNumber, pageSize));
        Result<GetUsersResponse> result = await handler.Handle(query, cancellationToken);

        return result.Match(onSuccess: response => Results.Ok(response));
    }
}
