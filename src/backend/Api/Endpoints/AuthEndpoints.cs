using Api.Extensions;
using Application.Auth;
using Application.Interfaces;
using Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

/// <summary>
/// Data transfer object for successful authentication responses.
/// </summary>
/// <param name="AccessToken">The JWT access token for API authentication.</param>
/// <param name="TokenType">The type of token (typically "Bearer").</param>
/// <param name="ExpiresIn">The access token expiration time (e.g., "3600" for 1 hour).</param>
public record SignInResponseDTO(string AccessToken, string TokenType, string ExpiresIn);

/// <summary>
/// Authentication endpoints for user registration, login, token refresh, and logout.
/// </summary>
public static class AuthEndpoints
{
    private const string REFRESH_TOKEN_COOKIE_NAME = "RefreshToken";
    private const int REFRESH_TOKEN_EXPIRY_DAYS = 7;

    /// <summary>
    /// Registers authentication-related endpoints in the application.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to register routes with.</param>
    public static void RegisterAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        IEndpointRouteBuilder authGroup = endpoints.MapGroup("/auth").WithTags("Authentication");

        authGroup
            .MapPost("/sign-in", SignInUser)
            .WithName("LoginUser")
            .AllowAnonymous()
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

        authGroup.MapPost("/sign-out", SignOutUser);
    }

    private static async Task<IResult> SignInUser(
        [FromBody] SignInCommand command,
        [FromServices] ICommandHandler<SignInCommand, SignInResponse> handler,
        HttpContext context,
        CancellationToken cancellationToken
    )
    {
        Result<SignInResponse> result = await handler.Handle(command, cancellationToken);
        return result.Match(onSuccess: loginResponse =>
        {
            SetRefreshTokenCookie(context, loginResponse.RefreshToken);
            return Results.Ok(
                new SignInResponseDTO(
                    loginResponse.AccessToken,
                    loginResponse.TokenType,
                    loginResponse.ExpiresIn
                )
            );
        });
    }

    private static async Task<IResult> RefreshUserToken(
        [FromServices] ICommandHandler<SignInWithRefreshTokenCommand, SignInResponse> handler,
        HttpContext context,
        CancellationToken cancellationToken
    )
    {
        if (
            !TryGetRefreshTokenFromCookie(context, out string? refreshToken) || refreshToken is null
        )
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Refresh token is missing or invalid."
            );
        }
        var command = new SignInWithRefreshTokenCommand(refreshToken);
        Result<SignInResponse> response = await handler.Handle(command, cancellationToken);

        return response.Match(onSuccess: loginResponse =>
        {
            SetRefreshTokenCookie(context, loginResponse.RefreshToken);
            return Results.Ok(
                new SignInResponseDTO(
                    loginResponse.AccessToken,
                    loginResponse.TokenType,
                    loginResponse.ExpiresIn
                )
            );
        });
    }

    private static async Task<IResult> SignOutUser(
        [FromServices] ICommandHandler<SignOutCommand, Guid> handler,
        HttpContext context,
        CancellationToken cancellationToken
    )
    {
        if (
            !TryGetRefreshTokenFromCookie(context, out string? refreshToken) || refreshToken is null
        )
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Refresh token is missing or invalid."
            );
        }
        var command = new SignOutCommand(refreshToken);
        Result<Guid> response = await handler.Handle(command, cancellationToken);
        return response.Match(onSuccess: userId =>
        {
            DeleteRefreshTokenCookie(context);
            return Results.Ok(new { UserId = userId });
        });
    }

    private static bool TryGetRefreshTokenFromCookie(HttpContext context, out string? refreshToken)
    {
        if (
            context.Request.Cookies.TryGetValue(REFRESH_TOKEN_COOKIE_NAME, out refreshToken)
            && !string.IsNullOrWhiteSpace(refreshToken)
        )
        {
            return true;
        }

        refreshToken = null;
        return false;
    }

    private static CookieOptions GetRefreshTokenCookieOptions(HttpContext context) =>
        new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7),
            Path = "/",
        };

    private static void SetRefreshTokenCookie(HttpContext context, string refreshToken) =>
        context.Response.Cookies.Append(
            REFRESH_TOKEN_COOKIE_NAME,
            refreshToken,
            GetRefreshTokenCookieOptions(context)
        );

    private static void DeleteRefreshTokenCookie(HttpContext context)
    {
        CookieOptions options = GetRefreshTokenCookieOptions(context);
        options.Expires = DateTime.UtcNow.AddSeconds(-1);
        context.Response.Cookies.Delete(REFRESH_TOKEN_COOKIE_NAME, options);
    }
}
