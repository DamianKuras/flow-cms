using Api.Extensions;
using Application.Auth;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

/// <summary>
/// Authentication endpoints for user registration and login.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Registers authentication-related endpoints in the application.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    public static void RegisterAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        IEndpointRouteBuilder authGroup = endpoints.MapGroup("/auth").WithTags("Authentication");

        authGroup
            .MapPost("/register", RegisterUser)
            .WithName("RegisterUser")
            .WithDescription("Register a new user account")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        authGroup
            .MapPost("/login", LoginUser)
            .WithName("LoginUser")
            .WithDescription("Authenticate a user and receive access tokens")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> RegisterUser(
        [FromBody] CreateUserCommand command,
        [FromServices] ICommandHandler<CreateUserCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        CommandResponse<Guid> response = await handler.Handle(command, cancellationToken);
        return response.Match(
            onSuccess: userId => Results.Created($"/api/users/{userId}", new { Id = userId }),
            onValidationFailure: ResultExtensions.MapValidationToResult,
            onFailure: ResultExtensions.MapErrorToResult
        );
    }

    private static async Task<IResult> LoginUser(
        [FromBody] SignInCommand command,
        [FromServices] ICommandHandler<SignInCommand, SingInResponseDTO> handler,
        CancellationToken cancellationToken
    )
    {
        CommandResponse<SingInResponseDTO> response = await handler.Handle(
            command,
            cancellationToken
        );
        return response.Match(
            onSuccess: loginResponseDto => Results.Ok(loginResponseDto),
            onValidationFailure: ResultExtensions.MapValidationToResult,
            onFailure: ResultExtensions.MapErrorToResult
        );
    }
}
