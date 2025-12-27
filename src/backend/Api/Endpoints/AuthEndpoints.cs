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
            .MapPost("/sign-in", SignInUser)
            .WithName("LoginUser")
            .WithDescription("Authenticate a user and receive access tokens")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

        authGroup
            .MapPost("/refresh-token", RefreshUserToken)
            .WithName("RefreshToken")
            .WithDescription("Receive new access token and refresh token from refresh token")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        authGroup.MapPost("/sing-out", SignOutUser);
    }

    private static async Task<IResult> SignInUser(
        [FromBody] SignInCommand command,
        [FromServices] ICommandHandler<SignInCommand, SignInResponseDTO> handler,
        CancellationToken cancellationToken
    )
    {
        CommandResponse<SignInResponseDTO> response = await handler.Handle(
            command,
            cancellationToken
        );
        return response.Match(
            onSuccess: loginResponseDto => Results.Ok(loginResponseDto),
            onValidationFailure: ResultExtensions.MapValidationToResult,
            onFailure: ResultExtensions.MapErrorToResult
        );
    }

    private static async Task<IResult> RefreshUserToken(
        [FromBody] SignInWithRefreshTokenCommand command,
        [FromServices] ICommandHandler<SignInWithRefreshTokenCommand, SignInResponseDTO> handler,
        CancellationToken cancellationToken
    )
    {
        CommandResponse<SignInResponseDTO> response = await handler.Handle(
            command,
            cancellationToken
        );

        return response.Match(
            onSuccess: loginResponseDto => Results.Ok(loginResponseDto),
            onValidationFailure: ResultExtensions.MapValidationToResult,
            onFailure: ResultExtensions.MapErrorToResult
        );
    }

    private static async Task<IResult> SignOutUser(
        [FromBody] SignOutCommand command,
        [FromServices] ICommandHandler<SignOutCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        CommandResponse<Guid> response = await handler.Handle(command, cancellationToken);
        return response.Match(
            onSuccess: userId => Results.Ok(new { UserId = userId }),
            onValidationFailure: ResultExtensions.MapValidationToResult,
            onFailure: ResultExtensions.MapErrorToResult
        );
    }
}
