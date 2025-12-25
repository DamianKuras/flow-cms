using Application.Interfaces;
using Domain.Common;
using Domain.Users;
using Microsoft.Extensions.Options;

namespace Application.Auth;

/// <summary>
/// Command for signing in a user using a refresh token.
/// </summary>
/// <param name="RefreshToken"></param>
public record SignInWithRefreshTokenCommand(string RefreshToken);

/// <summary>
/// Handles the refresh token authentication flow by validating the existing token,
/// revoking it, and issuing new access and refresh tokens.
/// </summary>
/// <param name="authService">Service for generating authentication tokens.</param>
/// <param name="refreshTokenRepository">Repository for managing refresh tokens.</param>
/// <param name="userRepository">Repository for retrieving user data.</param>
/// <param name="jwtOptions">Configuration options for JWT tokens.</param>
public sealed class SignInWithRefreshTokenCommandHandler(
    IAuthService authService,
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IOptions<JwtOptions> jwtOptions
) : ICommandHandler<SignInWithRefreshTokenCommand, SignInResponseDTO>
{
    async Task<CommandResponse<SignInResponseDTO>> ICommandHandler<
        SignInWithRefreshTokenCommand,
        SignInResponseDTO
    >.Handle(SignInWithRefreshTokenCommand command, CancellationToken cancellationToken)
    {
        RefreshToken? existingToken = await refreshTokenRepository.GetByTokenValueAsync(
            command.RefreshToken
        );
        if (
            existingToken is null
            || existingToken.IsRevoked
            || existingToken.ExpiresOnUtc < DateTime.UtcNow
        )
        {
            return CommandResponse<SignInResponseDTO>.Failure(
                Error.Unauthorized("The refresh token is invalid or expired.")
            );
        }
        existingToken.IsRevoked = true;

        string newRefreshToken = authService.GenerateRefreshToken();

        var dbNewRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = newRefreshToken,
            UserId = existingToken.UserId,
            ExpiresOnUtc = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationInDays),
        };

        await refreshTokenRepository.AddAsync(dbNewRefreshToken, cancellationToken);

        User? user = await userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
        string newAccessToken = await authService.GenerateAccessTokenAsync(user);

        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return CommandResponse<SignInResponseDTO>.Success(
            new SignInResponseDTO(
                AccessToken: newAccessToken,
                TokenType: "Bearer",
                ExpiresIn: Convert.ToString(jwtOptions.Value.ExpirationInMinutes * 60) ?? "3600",
                RefreshToken: newRefreshToken
            )
        );
    }
}
