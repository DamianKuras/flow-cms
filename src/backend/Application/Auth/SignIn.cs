using Application.Interfaces;
using Domain.Common;
using Domain.Users;
using Microsoft.Extensions.Options;

namespace Application.Auth;

/// <summary>
/// Command object containing user credentials for sign-in authentication.
/// </summary>
/// <param name="Email">The user's email address used as login identifier.</param>
/// <param name="Password">The user's password for authentication.</param>
public record SignInCommand(string Email, string Password);

/// <summary>
/// Handles the sign-in authentication command by validating user credentials
/// and generating JWT authentication tokens upon successful authentication.
/// </summary>
/// <param name="authorizationService">Service responsible for user authentication operations.</param>
/// <param name="jwtOptions"></param>
public sealed class SignInCommandHandler(
    IAuthService authorizationService,
    IOptions<JwtOptions> jwtOptions
) : ICommandHandler<SignInCommand, SignInResponseDTO>
{
    /// <summary>
    /// Processes the sign-in command by authenticating the user and generating authentication tokens.
    /// </summary>
    /// <param name="command">The sign-in command containing user credentials.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A command response containing the sign-in response DTO with authentication tokens on success,
    /// or an unauthorized error if authentication fails.
    /// </returns>
    public async Task<CommandResponse<SignInResponseDTO>> Handle(
        SignInCommand command,
        CancellationToken cancellationToken
    )
    {
        Result<string> result = await authorizationService.SignInAsync(command, cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            return CommandResponse<SignInResponseDTO>.Success(
                new SignInResponseDTO(
                    AccessToken: result.Value,
                    TokenType: "Bearer",
                    ExpiresIn: Convert.ToString(jwtOptions.Value.ExpirationInMinutes * 60)
                        ?? "3600",
                    RefreshToken: string.Empty, // TODO: Generate actual refresh token
                    Scope: "read write" // TODO: Determine scope based on user roles)
                )
            );
        }
        else
        {
            return CommandResponse<SignInResponseDTO>.Failure(
                Error.Unauthorized("Username or password is incorrect.")
            );
        }
    }
}
