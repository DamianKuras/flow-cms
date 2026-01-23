using Application.Interfaces;
using Domain.Common;
using Domain.Users;
using Microsoft.Extensions.Logging;

namespace Application.Auth;

/// <summary>
/// Command for signing out a user by revoking their refresh token.
/// </summary>
/// <param name="RefreshToken">The refresh token value to be revoked.</param>
public record SignOutCommand(string RefreshToken);

/// <summary>
/// Handler for signing out a user by revoking their refresh token.
/// </summary>
/// <param name="refreshTokenRepository">Repository for refresh token operations.</param>
/// <param name="logger">Logger for tracking sign-out operations.</param>
public sealed class SignOutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ILogger<SignOutCommandHandler> logger
) : ICommandHandler<SignOutCommand, Guid>
{
    /// <summary>
    /// Processes the sign-out command by locating and revoking the specified refresh token.
    /// </summary>
    /// <param name="command">The sign-out command containing the refresh token to revoke.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A command response containing the user ID if successful, or an error if the token is not found or invalid.
    /// </returns>
    public async Task<Result<Guid>> Handle(
        SignOutCommand command,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Handling SignOutCommand");
        // Validate input.
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            logger.LogWarning("Validation failed for SignOutCommand: Refresh token is empty");
            return Result<Guid>.Failure(Error.Validation("Refresh token cannot be empty."));
        }

        // Revoke token.
        RefreshToken? token = await refreshTokenRepository.GetByTokenValueAsync(
            command.RefreshToken,
            cancellationToken
        );

        if (token is null)
        {
            logger.LogWarning("SignOut failed: Refresh token not found.");
            return Result<Guid>.Failure(Error.Unauthorized("Refresh token not found."));
        }

        if (token.IsRevoked)
        {
            logger.LogInformation(
                "Refresh token already revoked for UserId={UserId}",
                token.UserId
            );
            return Result<Guid>.Success(token.UserId);
        }

        if (token.ExpiresOnUtc < DateTime.UtcNow)
        {
            logger.LogWarning(
                "SignOut failed: Refresh token expired for UserId={UserId} ExpiresAt={ExpiresAt}",
                token.UserId,
                token.ExpiresOnUtc
            );
            return Result<Guid>.Failure(Error.Unauthorized("Refresh token has expired."));
        }

        logger.LogInformation("Revoking refresh token for UserId={UserId}", token.UserId);
        token.Revoke();

        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully signed out UserId={UserId}", token.UserId);

        return Result<Guid>.Success(token.UserId);
    }
}
